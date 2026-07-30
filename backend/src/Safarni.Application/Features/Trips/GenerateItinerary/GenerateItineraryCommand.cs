using ChatBotApi.Application.Common.DTOs;
using ChatBotApi.Application.Common.Interfaces;
using ChatBotApi.Application.Common.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using Safarni.Application.Abstractions.Authentication;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Abstractions.Travel;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;
using Safarni.Domain.Entities;

namespace Safarni.Application.Features.Trips.GenerateItinerary;

public sealed record GenerateItineraryCommand(
    Guid TripId,
    GenerateItineraryPreferences Preferences,
    decimal BudgetAmount,
    string CurrencyCode,
    int TravelerCount)
    : IRequest<Result<ItineraryResponse>>;

public sealed class GenerateItineraryCommandHandler
    : IRequestHandler<GenerateItineraryCommand, Result<ItineraryResponse>>
{
    private const int MaximumGenerationAttempts = 2;
    private static readonly TimeSpan RateLimitRetryDelay =
        TimeSpan.FromSeconds(61);

    private readonly ICurrentUserService _currentUserService;
    private readonly ITripRepository _tripRepository;
    private readonly IItineraryRepository _itineraryRepository;
    private readonly IAIProvider _aiProvider;
    private readonly IGooglePlacesService _googlePlacesService;
    private readonly IItineraryTravelDataService?
        _travelDataService;
    private readonly IItineraryRouteService?
        _routeService;
    private readonly IFlightAirportService?
        _flightService;
    private readonly ILogger<GenerateItineraryCommandHandler>
        _logger;

    public GenerateItineraryCommandHandler(
        ICurrentUserService currentUserService,
        ITripRepository tripRepository,
        IItineraryRepository itineraryRepository,
        IAIProvider aiProvider,
        IGooglePlacesService googlePlacesService,
        ILogger<GenerateItineraryCommandHandler> logger,
        IItineraryTravelDataService? travelDataService = null,
        IItineraryRouteService? routeService = null,
        IFlightAirportService? flightService = null)
    {
        _currentUserService = currentUserService;
        _tripRepository = tripRepository;
        _itineraryRepository = itineraryRepository;
        _aiProvider = aiProvider;
        _googlePlacesService = googlePlacesService;
        _logger = logger;
        _travelDataService = travelDataService;
        _routeService = routeService;
        _flightService = flightService;
    }

    public async Task<Result<ItineraryResponse>> Handle(
        GenerateItineraryCommand request,
        CancellationToken cancellationToken)
    {
        var tripResult = await GetOwnedTripAsync(
            request.TripId,
            cancellationToken);

        if (tripResult.Error is not null)
        {
            return Result<ItineraryResponse>.Failure(tripResult.Error);
        }

        var trip = tripResult.Trip!;
        var currencyCode = request.CurrencyCode.Trim().ToUpperInvariant();

        try
        {
            var travelerCities =
                TripRouteSerializer.Deserialize(
                        trip.SelectedCitiesJson)
                    .Select(city =>
                        new ItineraryCitySelection(
                            city.Name,
                            city.Days))
                    .ToList();

            var cityPlan =
                await ItineraryCityPlanner.ResolveAsync(
                    trip,
                    _googlePlacesService,
                    travelerCities,
                    cancellationToken);

            var places = await ItineraryPlaceCatalog.LoadAsync(
                _googlePlacesService,
                _travelDataService,
                trip,
                cityPlan,
                request.Preferences,
                currencyCode,
                request.TravelerCount,
                cancellationToken);

            if (places.Count == 0)
            {
                return Result<ItineraryResponse>.Failure(
                    ItineraryErrors.PlacesUnavailable);
            }

            var generatedDays =
                await GenerateValidatedDaysAsync(
                    trip,
                    request,
                    currencyCode,
                    places,
                    cityPlan,
                    cancellationToken);

            var itinerary = ItineraryEntityFactory.Create(
                trip,
                request.Preferences,
                request.BudgetAmount,
                currencyCode,
                request.TravelerCount,
                generatedDays,
                places,
                DateTime.UtcNow);

            await ItineraryFlightEnricher
                .AddRecommendedFlightsAsync(
                    itinerary,
                    trip,
                    cityPlan,
                    request.Preferences,
                    _flightService,
                    cancellationToken);

            await ItineraryRouteEnricher.EnrichAsync(
                itinerary,
                request.Preferences,
                _routeService,
                cancellationToken);

            await ReplaceActiveItineraryAsync(
                itinerary,
                cancellationToken);

            return Result<ItineraryResponse>.Success(
                ItineraryResponseMapper.Map(itinerary));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (ItineraryOutputException exception)
        {
            _logger.LogWarning(
                exception,
                "Rejected generated itinerary for trip {TripId}: {Reason}",
                request.TripId,
                exception.Message);

            return Result<ItineraryResponse>.Failure(
                ItineraryErrors.InvalidAiResponse);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to generate itinerary for trip {TripId}.",
                request.TripId);

            return Result<ItineraryResponse>.Failure(
                ItineraryErrors.GenerationFailed);
        }
    }

    private async Task<IReadOnlyList<AiItineraryDay>>
        GenerateValidatedDaysAsync(
            Trip trip,
            GenerateItineraryCommand request,
            string currencyCode,
            IReadOnlyList<ItineraryPlaceCandidate> places,
            IReadOnlyList<ItineraryCityStop> cityPlan,
            CancellationToken cancellationToken)
    {
        var generatedDays =
            new List<AiItineraryDay>();
        var usedMealSuggestions =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);
        var totalTripDays = Math.Max(
            1,
            (trip.EndDate.Date -
             trip.StartDate.Date).Days + 1);

        for (var cityIndex = 0;
             cityIndex < cityPlan.Count;
             cityIndex++)
        {
            var cityStop = cityPlan[cityIndex];
            var cityPlaces = places
                .Where(candidate =>
                    string.Equals(
                        candidate.City,
                        cityStop.City,
                        StringComparison.OrdinalIgnoreCase))
                .ToList();

            var segmentBudget = decimal.Round(
                request.BudgetAmount *
                cityStop.DayCount /
                totalTripDays,
                2,
                MidpointRounding.AwayFromZero);

            var cityDays =
                await GenerateValidatedCitySegmentAsync(
                    trip,
                    request,
                    currencyCode,
                    cityPlaces,
                    cityStop,
                    cityIndex == 0
                        ? null
                        : cityPlan[cityIndex - 1].City,
                    segmentBudget,
                    usedMealSuggestions,
                    cancellationToken);

            var adjustedMealCount =
                ItineraryMealVarietyResolver
                    .ResolveCrossSegmentDuplicates(
                        cityDays,
                        cityStop.City,
                        request.Preferences
                            .PreferredLanguage,
                        usedMealSuggestions);

            if (adjustedMealCount > 0)
            {
                _logger.LogInformation(
                    "Replaced {MealCount} repeated meal suggestions in {City} for trip {TripId} without another AI request.",
                    adjustedMealCount,
                    cityStop.City,
                    request.TripId);
            }

            generatedDays.AddRange(cityDays);
        }

        ItineraryOutputParser.ValidateComplete(
            generatedDays,
            trip);
        ItineraryOutputParser.ValidatePlaceReferences(
            generatedDays,
            places,
            cityPlan);
        ItineraryOutputParser.ValidatePlanRules(
            generatedDays,
            request.Preferences,
            request.BudgetAmount,
            places,
            cityPlan);

        return generatedDays
            .OrderBy(day => day.DayNumber)
            .ToList();
    }

    private async Task<IReadOnlyList<AiItineraryDay>>
        GenerateValidatedCitySegmentAsync(
            Trip trip,
            GenerateItineraryCommand request,
            string currencyCode,
            IReadOnlyList<ItineraryPlaceCandidate> places,
            ItineraryCityStop cityStop,
            string? previousCity,
            decimal segmentBudget,
            IReadOnlyCollection<string>
                usedMealSuggestions,
            CancellationToken cancellationToken)
    {
        var originalUserMessage =
            ItineraryPromptBuilder.BuildUserMessage(
                trip,
                request.Preferences,
                segmentBudget,
                currencyCode,
                request.TravelerCount,
                places,
                [cityStop],
                cityStop,
                previousCity,
                usedMealSuggestions);

        ItineraryOutputException? lastRejection = null;

        for (var attempt = 1;
             attempt <= MaximumGenerationAttempts;
             attempt++)
        {
            var userMessage = lastRejection is null
                ? originalUserMessage
                : ItineraryPromptBuilder.BuildRetryUserMessage(
                    originalUserMessage,
                    lastRejection.Message);

            var output =
                await GetCompletionWithRateLimitRecoveryAsync(
                    ItineraryPromptBuilder.SystemPrompt,
                    userMessage,
                    AICompletionOptions.ItineraryPlanning,
                    request.TripId,
                    $"itinerary generation for {cityStop.City}",
                    cancellationToken);

            try
            {
                var generatedDays =
                    ItineraryOutputParser.ParseRange(
                        output,
                        trip,
                        cityStop.StartDayNumber,
                        cityStop.EndDayNumber);

                ItineraryOutputParser
                    .NormalizeChronologicalOrder(
                        generatedDays);

                var accommodationAdjustments =
                    ItineraryAccommodationResolver
                        .NormalizeCityStay(
                            generatedDays,
                            cityStop,
                            request.Preferences,
                            places);

                if (accommodationAdjustments > 0)
                {
                    _logger.LogInformation(
                        "Normalized {AdjustmentCount} accommodation items in {City} for trip {TripId} without another AI request.",
                        accommodationAdjustments,
                        cityStop.City,
                        request.TripId);
                }

                foreach (var generatedDay in generatedDays)
                {
                    ItineraryPlaceCatalog
                        .ResolvePlaceReferences(
                            generatedDay,
                            places,
                            cityStop.City);
                }

                ItineraryOutputParser
                    .ValidatePlaceReferences(
                        generatedDays,
                        places,
                        [cityStop]);

                ItineraryOutputParser
                    .ValidatePlanRules(
                        generatedDays,
                        request.Preferences,
                        request.BudgetAmount,
                        places,
                        [cityStop]);

                return generatedDays;
            }
            catch (ItineraryOutputException exception)
                when (attempt <
                      MaximumGenerationAttempts)
            {
                lastRejection = exception;

                _logger.LogInformation(
                    "Itinerary generation attempt {Attempt} for {City} in trip {TripId} was rejected: {Reason}. Retrying once.",
                    attempt,
                    cityStop.City,
                    request.TripId,
                    exception.Message);
            }
        }

        throw lastRejection ??
              new ItineraryOutputException(
                  "The generated itinerary failed validation.");
    }

    private async Task<string>
        GetCompletionWithRateLimitRecoveryAsync(
            string systemPrompt,
            string userMessage,
            AICompletionOptions options,
            Guid tripId,
            string operation,
            CancellationToken cancellationToken)
    {
        try
        {
            return await _aiProvider.GetChatCompletionAsync(
                systemPrompt,
                new List<ChatMessageDto>(),
                userMessage,
                options,
                cancellationToken);
        }
        catch (HttpRequestException exception)
            when (exception.Message.Contains(
                "rate limit",
                StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "Groq rate-limited {Operation} for trip {TripId}. Retrying once after {DelaySeconds} seconds.",
                operation,
                tripId,
                RateLimitRetryDelay.TotalSeconds);

            await Task.Delay(
                RateLimitRetryDelay,
                cancellationToken);

            return await _aiProvider.GetChatCompletionAsync(
                systemPrompt,
                new List<ChatMessageDto>(),
                userMessage,
                options,
                cancellationToken);
        }
    }

    private async Task<(Trip? Trip, Error? Error)> GetOwnedTripAsync(
        Guid tripId,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        if (userId is null)
        {
            return (
                null,
                AuthenticationErrors.AuthenticationRequired);
        }

        var trip = await _tripRepository.GetByIdAsync(
            tripId,
            cancellationToken);

        if (trip is null)
        {
            return (null, ItineraryErrors.TripNotFound);
        }

        return trip.UserId == userId.Value
            ? (trip, null)
            : (null, ItineraryErrors.Forbidden);
    }

    private async Task ReplaceActiveItineraryAsync(
        Itinerary itinerary,
        CancellationToken cancellationToken)
    {
        var previous =
            await _itineraryRepository.GetActiveByTripIdAsync(
                itinerary.TripId,
                cancellationToken);

        if (previous is not null)
        {
            previous.IsActive = false;
            previous.UpdatedAt = itinerary.CreatedAt;
        }

        await _itineraryRepository.AddAsync(
            itinerary,
            cancellationToken);

        await _itineraryRepository.SaveAsync(cancellationToken);
    }
}
