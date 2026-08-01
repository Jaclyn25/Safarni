using ChatBotApi.Application.Common.DTOs;
using ChatBotApi.Application.Common.Exceptions;
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
    private const int MaximumGenerationAttempts = 4;
    private const int MaximumRateLimitAttempts = 4;
    private static readonly TimeSpan DefaultRateLimitRetryDelay =
        TimeSpan.FromSeconds(20);

    private readonly ITripRepository _tripRepository;
    private readonly IItineraryRepository _itineraryRepository;
    private readonly IItineraryGenerationJobRepository? _jobs;
    private readonly IItineraryAIProvider _aiProvider;
    private readonly IGooglePlacesService _googlePlacesService;
    private readonly IItineraryTravelDataService? _travelDataService;
    private readonly IItineraryRouteService? _routeService;
    private readonly IFlightAirportService? _flightService;
    private readonly ILogger<ItineraryGenerationProcessor> _logger;

    public ItineraryGenerationProcessor(
        ITripRepository tripRepository,
        IItineraryRepository itineraryRepository,
        IItineraryAIProvider aiProvider,
        IGooglePlacesService googlePlacesService,
        ILogger<ItineraryGenerationProcessor> logger,
        IItineraryGenerationJobRepository? jobs = null,
        IItineraryTravelDataService? travelDataService = null,
        IItineraryRouteService? routeService = null,
        IFlightAirportService? flightService = null)
    {
        _tripRepository = tripRepository;
        _itineraryRepository = itineraryRepository;
        _aiProvider = aiProvider;
        _googlePlacesService = googlePlacesService;
        _logger = logger;
        _jobs = jobs;
        _travelDataService = travelDataService;
        _routeService = routeService;
        _flightService = flightService;
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

            var cityPlan = await ItineraryCityPlanner.ResolveAsync(
                trip,
                _googlePlacesService,
                travelerCities,
                cancellationToken);

            await ReportProgressAsync(
                generationJobId,
                12,
                "Resolving exact flight routes",
                cancellationToken);

            var flightPlan = await ItineraryFlightEnricher.ResolvePlanAsync(
                trip,
                cityPlan,
                normalizedCurrency,
                travelerCount,
                preferences.PreferredLanguage,
                _flightService,
                cancellationToken);

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
                16,
                "Suggesting activities for your preferences",
                cancellationToken);

            var activitySuggestions =
                await GenerateActivitySuggestionsAsync(
                    tripId,
                    cityPlan,
                    preferences,
                    cancellationToken);

            await ReportProgressAsync(
                generationJobId,
                20,
                "Verifying suggested activities",
                cancellationToken);

            var verifiedActivities =
                await ItineraryPlaceCatalog
                    .VerifyActivitySuggestionsAsync(
                        _googlePlacesService,
                        _travelDataService,
                        activitySuggestions,
                        cityPlan,
                        preferences.PreferredLanguage,
                        cancellationToken);

            await ReportProgressAsync(
                generationJobId,
                24,
                "Loading fallback places and hotel rates",
                cancellationToken);

            var fallbackPlaces =
                await ItineraryPlaceCatalog.LoadAsync(
                    _googlePlacesService,
                    _travelDataService,
                    trip,
                    cityPlan,
                    preferences,
                    normalizedCurrency,
                    travelerCount,
                    cancellationToken);

            var places = ItineraryPlaceCatalog
                .MergeSuggestedActivities(
                    fallbackPlaces,
                    verifiedActivities,
                    cityPlan,
                    preferences);

            _logger.LogInformation(
                "Prepared {ActivitySuggestionCount} AI activity suggestions, verified {VerifiedActivityCount}, and built a final catalog of {PlaceCount} places for trip {TripId}.",
                activitySuggestions.Count,
                verifiedActivities.Count,
                places.Count,
                tripId);

            if (places.Count == 0)
            {
                return Result<ItineraryResponse>.Failure(
                    ItineraryErrors.PlacesUnavailable);
            }

            var generatedDays = await GenerateValidatedDaysAsync(
                trip,
                tripId,
                preferences,
                planningBudget,
                normalizedCurrency,
                travelerCount,
                places,
                cityPlan,
                flightPlan,
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
        GenerateValidatedDaysAsync(
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
            cityPlan);
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
                tripId,
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
                        places);

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
                    [cityStop]);
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
            Guid tripId,
            IReadOnlyList<ItineraryCityStop> cityPlan,
            GenerateItineraryPreferences preferences,
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
                    tripId,
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
        Guid tripId,
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
                return await _aiProvider.GetChatCompletionAsync(
                    systemPrompt,
                    new List<ChatMessageDto>(),
                    userMessage,
                    options,
                    cancellationToken);
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

    private static AICompletionOptions BuildCompletionOptions(
        int dayCount)
    {
        var normalizedDayCount = Math.Max(1, dayCount);
        var maxTokens = Math.Clamp(
            1000 + (normalizedDayCount * 400),
            1400,
            3200);

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