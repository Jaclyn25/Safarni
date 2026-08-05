using System.Diagnostics;
using ChatBotApi.Application.Common.DTOs;
using ChatBotApi.Application.Common.Exceptions;
using ChatBotApi.Application.Common.Interfaces;
using ChatBotApi.Application.Common.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using Safarni.Application.Abstractions.AI;
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
    decimal? BudgetAmount,
    string CurrencyCode,
    int TravelerCount)
    : IRequest<Result<ItineraryResponse>>;

/// <summary>
/// Runs the deterministic itinerary pipeline for an authenticated owner.
/// The same processor is used by the legacy synchronous command and by the
/// durable background worker.
/// </summary>
public interface IItineraryGenerationProcessor
{
    Task<Result<ItineraryResponse>> GenerateAsync(
        Guid tripId,
        Guid userId,
        GenerateItineraryPreferences preferences,
        decimal? budgetAmount,
        string currencyCode,
        int travelerCount,
        Guid? generationJobId,
        CancellationToken cancellationToken);
}

public sealed class GenerateItineraryCommandHandler
    : IRequestHandler<GenerateItineraryCommand, Result<ItineraryResponse>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IItineraryGenerationProcessor _processor;

    public GenerateItineraryCommandHandler(
        ICurrentUserService currentUserService,
        IItineraryGenerationProcessor processor)
    {
        _currentUserService = currentUserService;
        _processor = processor;
    }

    public Task<Result<ItineraryResponse>> Handle(
        GenerateItineraryCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is not { } userId)
        {
            return Task.FromResult(
                Result<ItineraryResponse>.Failure(
                    AuthenticationErrors.AuthenticationRequired));
        }

        return _processor.GenerateAsync(
            request.TripId,
            userId,
            request.Preferences,
            request.BudgetAmount,
            request.CurrencyCode,
            request.TravelerCount,
            generationJobId: null,
            cancellationToken);
    }
}

public sealed class ItineraryGenerationProcessor
    : IItineraryGenerationProcessor
{
    private const int MaximumGenerationAttempts = 2;
    private const int MaximumRateLimitAttempts = 2;
    private static readonly TimeSpan DefaultRateLimitRetryDelay =
        TimeSpan.FromSeconds(20);

    private readonly ITripRepository _tripRepository;
    private readonly IItineraryRepository _itineraryRepository;
    private readonly IItineraryGenerationJobRepository? _jobs;
    private readonly IItineraryAIProvider _aiProvider;
    private readonly IItineraryTravelDataService? _travelDataService;
    private readonly IItineraryRouteService? _routeService;
    private readonly IFlightAirportService? _flightService;
    private readonly IAIRequestLogWriter? _telemetryWriter;
    private readonly ILogger<ItineraryGenerationProcessor> _logger;

    public ItineraryGenerationProcessor(
        ITripRepository tripRepository,
        IItineraryRepository itineraryRepository,
        IItineraryAIProvider aiProvider,
        ILogger<ItineraryGenerationProcessor> logger,
        IItineraryGenerationJobRepository? jobs = null,
        IItineraryTravelDataService? travelDataService = null,
        IItineraryRouteService? routeService = null,
        IFlightAirportService? flightService = null,
        IAIRequestLogWriter? telemetryWriter = null)
    {
        _tripRepository = tripRepository;
        _itineraryRepository = itineraryRepository;
        _aiProvider = aiProvider;
        _logger = logger;
        _jobs = jobs;
        _travelDataService = travelDataService;
        _routeService = routeService;
        _flightService = flightService;
        _telemetryWriter = telemetryWriter;
    }

    public async Task<Result<ItineraryResponse>> GenerateAsync(
        Guid tripId,
        Guid userId,
        GenerateItineraryPreferences preferences,
        decimal? budgetAmount,
        string currencyCode,
        int travelerCount,
        Guid? generationJobId,
        CancellationToken cancellationToken)
    {
        var trip = await _tripRepository.GetByIdAsync(
            tripId,
            cancellationToken);

        if (trip is null)
        {
            return Result<ItineraryResponse>.Failure(
                ItineraryErrors.TripNotFound);
        }

        if (trip.UserId != userId)
        {
            return Result<ItineraryResponse>.Failure(
                ItineraryErrors.Forbidden);
        }

        var normalizedCurrency = currencyCode.Trim().ToUpperInvariant();

        try
        {
            await ReportProgressAsync(
                generationJobId,
                5,
                "Validating trip route",
                cancellationToken);

            IReadOnlyList<TripCitySelection> travelerCities;
            try
            {
                travelerCities = TripRouteSerializer.ReadFromTrip(trip);
            }
            catch (InvalidOperationException)
            {
                return Result<ItineraryResponse>.Failure(
                    TripErrors.CorruptRoute);
            }

            if (travelerCities.Count == 0)
            {
                return Result<ItineraryResponse>.Failure(
                    TripErrors.CorruptRoute);
            }

            var cityPlan = ItineraryCityPlanner.ResolveTravelerCities(
                trip,
                travelerCities);

            await ReportProgressAsync(
                generationJobId,
                12,
                "Searching places, flights, and hotels in parallel",
                cancellationToken);

            var previousItineraries =
                await _itineraryRepository.GetRecentByTripIdAsync(
                    tripId,
                    maximumCount: 5,
                    cancellationToken);
            var previousItinerary = previousItineraries
                .FirstOrDefault(itinerary => itinerary.IsActive) ??
                previousItineraries.FirstOrDefault();
            var previouslyUsedPlaceIds = GetPreviouslyUsedPlaceIds(
                previousItineraries);

            var flightPlanTask = ItineraryFlightEnricher.ResolvePlanAsync(
                trip,
                cityPlan,
                normalizedCurrency,
                travelerCount,
                preferences.PreferredLanguage,
                _flightService,
                cancellationToken);

            var placeCatalogTask = ItineraryPlaceCatalog
                .LoadSerpPrimaryAsync(
                    _travelDataService,
                    trip,
                    cityPlan,
                    preferences,
                    normalizedCurrency,
                    travelerCount,
                    previouslyUsedPlaceIds,
                    cancellationToken);

            await Task.WhenAll(flightPlanTask, placeCatalogTask);

            var flightPlan = await flightPlanTask;
            var places = await placeCatalogTask;

            var knownFlightCost =
                (flightPlan.Outbound?.Price ?? 0m) +
                (flightPlan.Return?.Price ?? 0m);
            decimal? planningBudget = budgetAmount is null
                ? null
                : budgetAmount.Value - knownFlightCost;

            if (planningBudget is <= 0)
            {
                return Result<ItineraryResponse>.Failure(
                    ItineraryErrors.BudgetInsufficientForFlights);
            }

            await ReportProgressAsync(
                generationJobId,
                24,
                previousItinerary is null
                    ? "Preparing verified place catalog"
                    : "Preparing unused alternatives for regeneration",
                cancellationToken);

            _logger.LogInformation(
                "Built a SerpAPI-first catalog of {PlaceCount} verified places for trip {TripId}; regeneration={IsRegeneration}.",
                places.Count,
                tripId,
                previousItinerary is not null);

            if (places.Count == 0)
            {
                return Result<ItineraryResponse>.Failure(
                    ItineraryErrors.PlacesUnavailable);
            }

            await ReportProgressAsync(
                generationJobId,
                34,
                previousItinerary is null
                    ? "Planning the complete itinerary"
                    : "Regenerating every itinerary day",
                cancellationToken);

            var generatedDays = await GenerateValidatedTripAsync(
                userId,
                trip,
                tripId,
                preferences,
                planningBudget,
                normalizedCurrency,
                travelerCount,
                places,
                cityPlan,
                flightPlan,
                previousItinerary,
                generationJobId,
                cancellationToken);

            await ReportProgressAsync(
                generationJobId,
                78,
                "Building the saved itinerary",
                cancellationToken);

            var itinerary = ItineraryEntityFactory.Create(
                trip,
                preferences,
                budgetAmount,
                normalizedCurrency,
                travelerCount,
                generatedDays,
                places,
                DateTime.UtcNow);

            ItineraryFlightEnricher.AddFlights(
                itinerary,
                trip,
                cityPlan,
                preferences,
                flightPlan);

            ValidateFinalKnownBudget(itinerary, budgetAmount);

            await ReportProgressAsync(
                generationJobId,
                86,
                "Calculating routes",
                cancellationToken);

            await ItineraryRouteEnricher.EnrichAsync(
                itinerary,
                preferences,
                _routeService,
                cancellationToken);

            await ReportProgressAsync(
                generationJobId,
                94,
                "Saving itinerary",
                cancellationToken);

            await _itineraryRepository.ReplaceActiveAsync(
                itinerary,
                cancellationToken);

            await LinkSavedItineraryToJobAsync(
                generationJobId,
                itinerary.Id,
                cancellationToken);

            return Result<ItineraryResponse>.Success(
                ItineraryResponseMapper.Map(itinerary));
        }
        catch (ItineraryGenerationCancelledException)
        {
            throw;
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
                tripId,
                exception.Message);

            return Result<ItineraryResponse>.Failure(
                ItineraryErrors.InvalidAiResponse);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to generate itinerary for trip {TripId}.",
                tripId);

            return Result<ItineraryResponse>.Failure(
                ItineraryErrors.GenerationFailed);
        }
    }

    private async Task<IReadOnlyList<AiItineraryDay>>
        GenerateValidatedTripAsync(
            Guid userId,
            Trip trip,
            Guid tripId,
            GenerateItineraryPreferences preferences,
            decimal? budgetAmount,
            string currencyCode,
            int travelerCount,
            IReadOnlyList<ItineraryPlaceCandidate> places,
            IReadOnlyList<ItineraryCityStop> cityPlan,
            ItineraryFlightPlan flightPlan,
            Itinerary? previousItinerary,
            Guid? generationJobId,
            CancellationToken cancellationToken)
    {
        var totalTripDays = Math.Max(
            1,
            (trip.EndDate.Date - trip.StartDate.Date).Days + 1);
        var previousMealSuggestions = previousItinerary?.Days
            .SelectMany(day => day.Items)
            .Where(item =>
                item.Category == ItineraryItemCategory.Meal)
            .Select(item =>
                ItineraryOutputParser.GetMealSuggestionKey(
                    item.Title))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToHashSet(StringComparer.OrdinalIgnoreCase) ??
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var previousPlaceNames = previousItinerary?.Days
            .SelectMany(day => day.Items)
            .Where(item =>
                item.Category is ItineraryItemCategory.Activity or
                    ItineraryItemCategory.Meal or
                    ItineraryItemCategory.Accommodation)
            .Select(item => item.Location ?? item.Title)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(60)
            .ToList() ?? [];

        var originalUserMessage = ItineraryPromptBuilder.BuildUserMessage(
            trip,
            preferences,
            budgetAmount,
            currencyCode,
            travelerCount,
            places,
            cityPlan,
            generationStop: null,
            previousCity: null,
            excludedMealSuggestions: previousMealSuggestions,
            flightPlan: flightPlan,
            excludedPlaceNames: previousPlaceNames,
            isRegeneration: previousItinerary is not null);

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
            var output = await GetCompletionWithRateLimitRecoveryAsync(
                ItineraryPromptBuilder.SystemPrompt,
                userMessage,
                BuildCompletionOptions(totalTripDays),
                userId,
                tripId,
                generationJobId,
                previousItinerary is null
                    ? "full itinerary generation"
                    : "full itinerary regeneration",
                cancellationToken);

            try
            {
                var generatedDays = ItineraryOutputParser.ParseRange(
                    output,
                    trip,
                    1,
                    totalTripDays);
                ItineraryOutputParser.NormalizeChronologicalOrder(
                    generatedDays);
                ItineraryFlightWindowRepair.Repair(
                    generatedDays,
                    flightPlan,
                    totalTripDays);

                foreach (var cityStop in cityPlan)
                {
                    var cityDays = generatedDays
                        .Where(day =>
                            day.DayNumber >= cityStop.StartDayNumber &&
                            day.DayNumber <= cityStop.EndDayNumber)
                        .ToList();

                    ItineraryAccommodationResolver.NormalizeCityStay(
                        cityDays,
                        cityStop,
                        preferences,
                        places,
                        flightPlan,
                        totalTripDays);

                    foreach (var day in cityDays)
                    {
                        ItineraryPlaceCatalog.ResolvePlaceReferences(
                            day,
                            places,
                            cityStop.City);
                    }

                    ItineraryPlaceCatalog.ReconcileActivityItems(
                        cityDays,
                        places,
                        cityStop.City,
                        preferences.PreferredLanguage);
                    ItineraryDuplicatePlaceResolver.ReplaceDuplicates(
                        cityDays,
                        cityStop.City,
                        preferences.PreferredLanguage,
                        places);
                    var minimumActivityRepair =
                        ItineraryMinimumActivityResolver.Ensure(
                            cityDays,
                            places,
                            cityStop,
                            preferences.PreferredLanguage,
                            flightPlan,
                            totalTripDays);

                    if (minimumActivityRepair.Status ==
                        ItineraryMinimumActivityRepairStatus.Inserted)
                    {
                        _logger.LogInformation(
                            "Inserted verified fallback activity {PlaceName} ({PlaceReference}) in {City} on day {DayNumber} from {StartTime} to {EndTime}. Removed lower-priority items: {RemovedItems}.",
                            minimumActivityRepair.PlaceName,
                            minimumActivityRepair.PlaceReference,
                            minimumActivityRepair.City,
                            minimumActivityRepair.DayNumber,
                            minimumActivityRepair.Start,
                            minimumActivityRepair.End,
                            minimumActivityRepair.RemovedItems is { Count: > 0 }
                                ? string.Join(", ", minimumActivityRepair.RemovedItems)
                                : "none");
                    }
                    else if (minimumActivityRepair.Status ==
                             ItineraryMinimumActivityRepairStatus.NoFeasibleWindow)
                    {
                        _logger.LogInformation(
                            "No verified activity was inserted in {City} because protected transport or accommodation blocks leave no feasible sightseeing window.",
                            minimumActivityRepair.City);
                    }
                    else if (minimumActivityRepair.Status ==
                             ItineraryMinimumActivityRepairStatus.NoEligibleDay)
                    {
                        _logger.LogInformation(
                            "No verified activity is required in {City} because its assigned days have no sightseeing window after applying verified flight times.",
                            minimumActivityRepair.City);
                    }
                    else if (minimumActivityRepair.Status ==
                             ItineraryMinimumActivityRepairStatus.NoVerifiedCandidate)
                    {
                        _logger.LogInformation(
                            "No verified activity was inserted in {City} because the external discovery catalog returned no activity candidate.",
                            minimumActivityRepair.City);
                    }
                }

                ItineraryPlaceCatalog.ValidateRemainingOtherItems(
                    generatedDays);
                ItineraryOutputParser.ValidateComplete(
                    generatedDays,
                    trip);
                ItineraryOutputParser.ValidatePlaceReferences(
                    generatedDays,
                    places,
                    cityPlan);
                ItineraryOutputParser.ValidatePlanRules(
                    generatedDays,
                    preferences,
                    budgetAmount,
                    places,
                    cityPlan,
                    flightPlan,
                    totalTripDays);
                ItineraryOutputParser.ValidateFlightConstraints(
                    generatedDays,
                    flightPlan,
                    totalTripDays);
                ValidateRegenerationDifference(
                    previousItinerary,
                    generatedDays,
                    places);

                return generatedDays
                    .OrderBy(day => day.DayNumber)
                    .ToList();
            }

            catch (ItineraryOutputException exception)
                when (attempt < MaximumGenerationAttempts)
            {
                lastRejection = exception;
                var retryDelay = ResolveValidationRetryDelay(attempt);

                _logger.LogInformation(
                    "Full itinerary generation attempt {Attempt} for trip {TripId} was rejected: {Reason}. Retrying after {DelaySeconds} seconds.",
                    attempt,
                    tripId,
                    exception.Message,
                    retryDelay.TotalSeconds);
                await Task.Delay(retryDelay, cancellationToken);
            }
        }

        throw lastRejection ??
              new ItineraryOutputException(
                  "The generated itinerary failed validation.");
    }

    private static IReadOnlySet<string> GetPreviouslyUsedPlaceIds(
        Itinerary? itinerary)
    {
        return GetPreviouslyUsedPlaceIds(
            itinerary is null ? [] : [itinerary]);
    }

    private static IReadOnlySet<string> GetPreviouslyUsedPlaceIds(
        IEnumerable<Itinerary> itineraries)
    {
        return itineraries
            .SelectMany(itinerary => itinerary.Days)
            .SelectMany(day => day.Items)
            .Where(item =>
                item.Category is ItineraryItemCategory.Activity or
                    ItineraryItemCategory.Meal or
                    ItineraryItemCategory.Accommodation)
            .Select(item => item.GooglePlaceId)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static void ValidateRegenerationDifference(
        Itinerary? previousItinerary,
        IReadOnlyList<AiItineraryDay> generatedDays,
        IReadOnlyList<ItineraryPlaceCandidate> places)
    {
        var previousIds = GetPreviouslyUsedPlaceIds(previousItinerary);
        if (previousIds.Count == 0)
        {
            return;
        }

        var candidatesByReference = places.ToDictionary(
            candidate => candidate.Reference,
            StringComparer.OrdinalIgnoreCase);
        var generatedIds = generatedDays
            .SelectMany(day => day.Items)
            .Where(item =>
                ItineraryItemInput.ParseCategory(item.Category) is
                    ItineraryItemCategory.Activity or
                    ItineraryItemCategory.Meal)
            .Select(item =>
                string.IsNullOrWhiteSpace(item.PlaceReference) ||
                !candidatesByReference.TryGetValue(
                    item.PlaceReference.Trim(),
                    out var candidate)
                    ? null
                    : candidate.Place.PlaceId)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (generatedIds.Count == 0)
        {
            return;
        }

        var availableUnusedCount = places
            .Where(candidate =>
                (candidate.Purpose is ItineraryPlacePurpose.Activity or
                    ItineraryPlacePurpose.Meal) &&
                !previousIds.Contains(candidate.Place.PlaceId))
            .Select(candidate => candidate.Place.PlaceId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        var requiredDifferentCount = Math.Min(
            availableUnusedCount,
            (int)Math.Ceiling(generatedIds.Count * 0.70d));
        var actualDifferentCount = generatedIds.Count(id =>
            !previousIds.Contains(id));

        if (actualDifferentCount < requiredDifferentCount)
        {
            throw new ItineraryOutputException(
                "The regenerated itinerary is too similar to the previous version. " +
                $"Use at least {requiredDifferentCount} unused verified places.");
        }
    }

    private async Task<IReadOnlyList<AiItineraryDay>>
        GenerateValidatedDaysAsync(
            Guid userId,
            Trip trip,
            Guid tripId,
            GenerateItineraryPreferences preferences,
            decimal? budgetAmount,
            string currencyCode,
            int travelerCount,
            IReadOnlyList<ItineraryPlaceCandidate> places,
            IReadOnlyList<ItineraryCityStop> cityPlan,
            ItineraryFlightPlan flightPlan,
            Guid? generationJobId,
            CancellationToken cancellationToken)
    {
        var generatedDays = new List<AiItineraryDay>();
        var usedMealSuggestions = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);
        var totalTripDays = Math.Max(
            1,
            (trip.EndDate.Date - trip.StartDate.Date).Days + 1);

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

            decimal? segmentBudget = budgetAmount is null
                ? null
                : decimal.Round(
                    budgetAmount.Value *
                    cityStop.DayCount /
                    totalTripDays,
                    2,
                    MidpointRounding.AwayFromZero);

            var segmentProgress = 30 +
                (int)Math.Round(
                    cityIndex * 42d /
                    Math.Max(1, cityPlan.Count));

            await ReportProgressAsync(
                generationJobId,
                segmentProgress,
                $"Planning {cityStop.City}",
                cancellationToken);

            var cityDays = await GenerateValidatedCitySegmentAsync(
                userId,
                trip,
                tripId,
                preferences,
                currencyCode,
                travelerCount,
                cityPlaces,
                cityStop,
                cityIndex == 0
                    ? null
                    : cityPlan[cityIndex - 1].City,
                segmentBudget,
                usedMealSuggestions,
                flightPlan,
                generationJobId,
                cancellationToken);

            var adjustedMealCount = ItineraryMealVarietyResolver
                .ResolveCrossSegmentDuplicates(
                    cityDays,
                    cityStop.City,
                    preferences.PreferredLanguage,
                    usedMealSuggestions);

            if (adjustedMealCount > 0)
            {
                _logger.LogInformation(
                    "Replaced {MealCount} repeated meal suggestions in {City} for trip {TripId} without another AI request.",
                    adjustedMealCount,
                    cityStop.City,
                    tripId);
            }

            generatedDays.AddRange(cityDays);
        }

        ItineraryOutputParser.ValidateComplete(generatedDays, trip);
        ItineraryOutputParser.ValidatePlaceReferences(
            generatedDays,
            places,
            cityPlan);
        ItineraryOutputParser.ValidatePlanRules(
            generatedDays,
            preferences,
            budgetAmount,
            places,
            cityPlan,
            flightPlan,
            totalTripDays);
        ItineraryOutputParser.ValidateFlightConstraints(
            generatedDays,
            flightPlan,
            totalTripDays);

        return generatedDays
            .OrderBy(day => day.DayNumber)
            .ToList();
    }

    private async Task<IReadOnlyList<AiItineraryDay>>
        GenerateValidatedCitySegmentAsync(
            Guid userId,
            Trip trip,
            Guid tripId,
            GenerateItineraryPreferences preferences,
            string currencyCode,
            int travelerCount,
            IReadOnlyList<ItineraryPlaceCandidate> places,
            ItineraryCityStop cityStop,
            string? previousCity,
            decimal? segmentBudget,
            IReadOnlyCollection<string> usedMealSuggestions,
            ItineraryFlightPlan flightPlan,
            Guid? generationJobId,
            CancellationToken cancellationToken)
    {
        var originalUserMessage = ItineraryPromptBuilder.BuildUserMessage(
            trip,
            preferences,
            segmentBudget,
            currencyCode,
            travelerCount,
            places,
            [cityStop],
            cityStop,
            previousCity,
            usedMealSuggestions,
            flightPlan);

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

            var completionOptions = BuildCompletionOptions(
                cityStop.DayCount);

            var output = await GetCompletionWithRateLimitRecoveryAsync(
                ItineraryPromptBuilder.SystemPrompt,
                userMessage,
                completionOptions,
                userId,
                tripId,
                generationJobId,
                $"itinerary generation for {cityStop.City}",
                cancellationToken);

            try
            {
                var generatedDays = ItineraryOutputParser.ParseRange(
                    output,
                    trip,
                    cityStop.StartDayNumber,
                    cityStop.EndDayNumber);

                ItineraryOutputParser.NormalizeChronologicalOrder(
                    generatedDays);

                var totalTripDays = Math.Max(
                    1,
                    (trip.EndDate.Date - trip.StartDate.Date).Days + 1);

                var flightWindowAdjustments =
                    ItineraryFlightWindowRepair.Repair(
                        generatedDays,
                        flightPlan,
                        totalTripDays);

                if (flightWindowAdjustments > 0)
                {
                    _logger.LogInformation(
                        "Repaired {AdjustmentCount} itinerary items to respect verified flight windows in {City} for trip {TripId} without another AI request.",
                        flightWindowAdjustments,
                        cityStop.City,
                        tripId);

                    ItineraryOutputParser.NormalizeChronologicalOrder(
                        generatedDays);
                }

                var accommodationAdjustments = ItineraryAccommodationResolver
                    .NormalizeCityStay(
                        generatedDays,
                        cityStop,
                        preferences,
                        places,
                        flightPlan,
                        totalTripDays);

                if (accommodationAdjustments > 0)
                {
                    _logger.LogInformation(
                        "Normalized {AdjustmentCount} accommodation items in {City} for trip {TripId} without another AI request.",
                        accommodationAdjustments,
                        cityStop.City,
                        tripId);
                }

                foreach (var generatedDay in generatedDays)
                {
                    ItineraryPlaceCatalog.ResolvePlaceReferences(
                        generatedDay,
                        places,
                        cityStop.City);
                }

                var normalizedActivityCount =
                    ItineraryPlaceCatalog.NormalizeActivityItems(
                        generatedDays,
                        places,
                        cityStop.City,
                        preferences.PreferredLanguage);

                if (normalizedActivityCount > 0)
                {
                    _logger.LogInformation(
                        "Converted {ActivityCount} activity-like 'other' items into verified activities in {City} for trip {TripId}.",
                        normalizedActivityCount,
                        cityStop.City,
                        tripId);
                }

                ItineraryPlaceCatalog.ValidateRemainingOtherItems(
                    generatedDays);

                var duplicateReplacements = ItineraryDuplicatePlaceResolver
                    .ReplaceDuplicates(
                        generatedDays,
                        cityStop.City,
                        preferences.PreferredLanguage,
                        places);

                if (duplicateReplacements > 0)
                {
                    _logger.LogInformation(
                        "Replaced {PlaceCount} repeated verified places in {City} for trip {TripId} without another AI request.",
                        duplicateReplacements,
                        cityStop.City,
                        tripId);
                }

                ItineraryOutputParser.ValidatePlaceReferences(
                    generatedDays,
                    places,
                    [cityStop]);
                ItineraryOutputParser.ValidatePlanRules(
                    generatedDays,
                    preferences,
                    segmentBudget,
                    places,
                    [cityStop],
                    flightPlan,
                    totalTripDays);
                ItineraryOutputParser.ValidateFlightConstraints(
                    generatedDays,
                    flightPlan,
                    totalTripDays);

                return generatedDays;
            }
            catch (ItineraryOutputException exception)
                when (attempt < MaximumGenerationAttempts)
            {
                lastRejection = exception;

                var retryDelay = ResolveValidationRetryDelay(attempt);

                _logger.LogInformation(
                    "Itinerary generation attempt {Attempt} for {City} in trip {TripId} was rejected: {Reason}. Retrying after {DelaySeconds} seconds.",
                    attempt,
                    cityStop.City,
                    tripId,
                    exception.Message,
                    retryDelay.TotalSeconds);

                await Task.Delay(retryDelay, cancellationToken);
            }
        }

        throw lastRejection ??
              new ItineraryOutputException(
                  "The generated itinerary failed validation.");
    }

    private async Task<IReadOnlyList<ItineraryActivitySuggestion>>
        GenerateActivitySuggestionsAsync(
            Guid userId,
            Guid tripId,
            IReadOnlyList<ItineraryCityStop> cityPlan,
            GenerateItineraryPreferences preferences,
            Guid? generationJobId,
            CancellationToken cancellationToken)
    {
        var userMessage =
            ItineraryActivityDiscovery.BuildUserMessage(
                cityPlan,
                preferences);

        var options = new AICompletionOptions(
            Temperature: 0.35,
            MaxTokens:
                ItineraryActivityDiscovery
                    .GetRecommendedMaxTokens(
                        cityPlan,
                        preferences.Pace),
            TopP: 0.9,
            ReasoningEffort: "low");

        try
        {
            var output =
                await GetCompletionWithRateLimitRecoveryAsync(
                    ItineraryActivityDiscovery.SystemPrompt,
                    userMessage,
                    options,
                    userId,
                    tripId,
                    generationJobId,
                    "activity discovery",
                    cancellationToken);

            return ItineraryActivityDiscovery.Parse(
                output,
                cityPlan,
                preferences);
        }
        catch (AIProviderRateLimitException exception)
        {
            _logger.LogWarning(
                exception,
                "Activity discovery was rate-limited for trip {TripId}. The pipeline will use verified API fallback activities.",
                tripId);

            return [];
        }
        catch (ItineraryOutputException exception)
        {
            _logger.LogWarning(
                exception,
                "Activity discovery returned unusable suggestions for trip {TripId}. The pipeline will use verified API fallback activities.",
                tripId);

            return [];
        }
    }

    private async Task<string> GetCompletionWithRateLimitRecoveryAsync(
        string systemPrompt,
        string userMessage,
        AICompletionOptions options,
        Guid userId,
        Guid tripId,
        Guid? generationJobId,
        string operation,
        CancellationToken cancellationToken)
    {
        AIProviderRateLimitException? lastRateLimitException = null;

        for (var attempt = 1;
             attempt <= MaximumRateLimitAttempts;
             attempt++)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();

                try
                {
                    var output = await _aiProvider.GetChatCompletionAsync(
                        systemPrompt,
                        new List<ChatMessageDto>(),
                        userMessage,
                        options,
                        cancellationToken);

                    stopwatch.Stop();

                    await AIRequestTelemetry.RecordAsync(
                        _telemetryWriter,
                        new AIRequestLogEntry(
                            userId,
                            ConversationId: null,
                            generationJobId,
                            _aiProvider.ProviderName,
                            _aiProvider.ModelName,
                            operation,
                            Status: "Success",
                            stopwatch.ElapsedMilliseconds),
                        cancellationToken);

                    return output;
                }
                catch (Exception exception)
                {
                    stopwatch.Stop();

                    await AIRequestTelemetry.RecordAsync(
                        _telemetryWriter,
                        new AIRequestLogEntry(
                            userId,
                            ConversationId: null,
                            generationJobId,
                            _aiProvider.ProviderName,
                            _aiProvider.ModelName,
                            operation,
                            Status: "Failure",
                            stopwatch.ElapsedMilliseconds,
                            ErrorCode: exception.GetType().Name),
                        cancellationToken);

                    throw;
                }
            }
            catch (AIProviderRateLimitException exception)
            {
                lastRateLimitException = exception;

                if (attempt >= MaximumRateLimitAttempts)
                {
                    break;
                }

                var providerDelay = exception.RetryAfter is { } retryAfter &&
                                    retryAfter > TimeSpan.Zero
                    ? retryAfter + TimeSpan.FromSeconds(2)
                    : DefaultRateLimitRetryDelay;

                var progressiveMinimum = TimeSpan.FromSeconds(
                    attempt switch
                    {
                        1 => 15,
                        2 => 30,
                        3 => 45,
                        _ => 60
                    });

                var retryDelay = providerDelay > progressiveMinimum
                    ? providerDelay
                    : progressiveMinimum;

                if (retryDelay > TimeSpan.FromSeconds(90))
                {
                    retryDelay = TimeSpan.FromSeconds(90);
                }

                _logger.LogWarning(
                    "Itinerary AI provider rate-limited {Operation} for trip {TripId} on attempt {Attempt}. Retrying after {DelaySeconds} seconds.",
                    operation,
                    tripId,
                    attempt,
                    retryDelay.TotalSeconds);

                await Task.Delay(retryDelay, cancellationToken);
            }
        }

        if (lastRateLimitException is not null)
        {
            throw lastRateLimitException;
        }

        throw new InvalidOperationException(
            "The itinerary AI request ended without a result.");
    }

    internal static AICompletionOptions BuildCompletionOptions(
        int dayCount)
    {
        var normalizedDayCount = Math.Max(1, dayCount);
        var maxTokens = Math.Clamp(
            1200 + (normalizedDayCount * 650),
            1800,
            6000);

        return AICompletionOptions.ItineraryPlanning with
        {
            MaxTokens = maxTokens
        };
    }

    private static TimeSpan ResolveValidationRetryDelay(
        int completedAttempt)
    {
        return TimeSpan.FromSeconds(
            completedAttempt switch
            {
                1 => 8,
                2 => 15,
                3 => 25,
                _ => 30
            });
    }

    private static void ValidateFinalKnownBudget(
        Itinerary itinerary,
        decimal? budgetAmount)
    {
        if (budgetAmount is null)
        {
            return;
        }

        var knownTotal = itinerary.Days
            .SelectMany(day => day.Items)
            .Sum(item => item.EstimatedCost ?? 0m);

        if (knownTotal > budgetAmount.Value)
        {
            throw new ItineraryOutputException(
                $"The final known itinerary estimate ({knownTotal:0.00}) exceeds the traveler budget ({budgetAmount.Value:0.00}) after verified flight prices were added.");
        }
    }

    private async Task LinkSavedItineraryToJobAsync(
        Guid? jobId,
        Guid itineraryId,
        CancellationToken cancellationToken)
    {
        if (jobId is null || _jobs is null)
        {
            return;
        }

        var job = await _jobs.GetByIdAsync(
            jobId.Value,
            cancellationToken);

        if (job is null)
        {
            return;
        }

        if (job.Status == ItineraryGenerationJobStatus.Cancelled)
        {
            throw new ItineraryGenerationCancelledException();
        }

        job.ItineraryId = itineraryId;
        job.ProgressPercent = Math.Max(job.ProgressPercent, 99);
        job.Stage = "Finalizing itinerary";
        job.UpdatedAt = DateTime.UtcNow;
        await _jobs.SaveAsync(cancellationToken);
    }

    private async Task ReportProgressAsync(
        Guid? jobId,
        int percent,
        string stage,
        CancellationToken cancellationToken)
    {
        if (jobId is null || _jobs is null)
        {
            return;
        }

        var job = await _jobs.GetByIdAsync(
            jobId.Value,
            cancellationToken);

        if (job is null)
        {
            return;
        }

        if (job.Status == ItineraryGenerationJobStatus.Cancelled)
        {
            throw new ItineraryGenerationCancelledException();
        }

        job.ProgressPercent = Math.Clamp(
            Math.Max(job.ProgressPercent, percent),
            0,
            99);
        job.Stage = stage;
        job.UpdatedAt = DateTime.UtcNow;
        await _jobs.SaveAsync(cancellationToken);
    }
}

public sealed class ItineraryGenerationCancelledException : Exception
{
    public ItineraryGenerationCancelledException()
        : base("Itinerary generation was cancelled.")
    {
    }
}
