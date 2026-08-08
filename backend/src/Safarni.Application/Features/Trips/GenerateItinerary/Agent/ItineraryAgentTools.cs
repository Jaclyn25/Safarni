using System.Text.Json;
using ChatBotApi.Application.Common.Models.GooglePlaces;
using Safarni.Application.Abstractions.AI;
using Safarni.Application.Abstractions.Travel;
using Safarni.Application.Features.Trips.GenerateItinerary;
using Safarni.Domain.Entities;

namespace Safarni.Application.Features.Trips.GenerateItinerary.Agent;

internal interface IItineraryAgentTool
{
    ItineraryAgentToolDefinition Definition { get; }

    Task<ItineraryAgentToolExecution> ExecuteAsync(
        ItineraryAgentContext context,
        string argumentsJson,
        CancellationToken cancellationToken);
}

internal sealed class SearchFlightsAgentTool : IItineraryAgentTool
{
    private readonly Trip _trip;
    private readonly IReadOnlyList<ItineraryCityStop> _cityPlan;
    private readonly string _currencyCode;
    private readonly int _travelerCount;
    private readonly string _languageCode;
    private readonly IFlightAirportService? _flightService;

    public SearchFlightsAgentTool(
        Trip trip,
        IReadOnlyList<ItineraryCityStop> cityPlan,
        string currencyCode,
        int travelerCount,
        string languageCode,
        IFlightAirportService? flightService)
    {
        _trip = trip;
        _cityPlan = cityPlan;
        _currencyCode = currencyCode;
        _travelerCount = travelerCount;
        _languageCode = languageCode;
        _flightService = flightService;
    }

    public ItineraryAgentToolDefinition Definition { get; } = new(
        "search_flights",
        "Search the exact traveler-selected outbound and return airport routes. Use when verified flight timing or cost affects itinerary feasibility.",
        """
        {
          "type": "object",
          "properties": {}
        }
        """);

    public async Task<ItineraryAgentToolExecution> ExecuteAsync(
        ItineraryAgentContext context,
        string argumentsJson,
        CancellationToken cancellationToken)
    {
        if (context.FlightSearchAttempted)
        {
            return new ItineraryAgentToolExecution(
                JsonSerializer.Serialize(new
                {
                    cached = true,
                    flightPlan = FormatFlightPlan(context.FlightPlan)
                }),
                "Agent reused verified flight evidence");
        }

        context.FlightSearchAttempted = true;
        context.FlightPlan = await ItineraryFlightEnricher.ResolvePlanAsync(
            _trip,
            _cityPlan,
            _currencyCode,
            _travelerCount,
            _languageCode,
            _flightService,
            cancellationToken);
        context.RecordAction("search_flights");
        context.ClearValidation();

        return new ItineraryAgentToolExecution(
            JsonSerializer.Serialize(new
            {
                configured = _flightService?.IsConfigured == true,
                flightPlan = FormatFlightPlan(context.FlightPlan)
            }),
            "Agent checked verified flights");
    }

    private static object FormatFlightPlan(ItineraryFlightPlan plan)
    {
        return new
        {
            outbound = plan.Outbound is null
                ? null
                : new
                {
                    route = $"{plan.Outbound.DepartureAirportCode}-{plan.Outbound.ArrivalAirportCode}",
                    plan.Outbound.DepartureTime,
                    plan.Outbound.ArrivalTime,
                    plan.Outbound.Airlines,
                    plan.Outbound.FlightNumbers,
                    plan.Outbound.TotalDurationMinutes,
                    plan.Outbound.StopCount,
                    plan.Outbound.Price,
                    plan.Outbound.CurrencyCode
                },
            @return = plan.Return is null
                ? null
                : new
                {
                    route = $"{plan.Return.DepartureAirportCode}-{plan.Return.ArrivalAirportCode}",
                    plan.Return.DepartureTime,
                    plan.Return.ArrivalTime,
                    plan.Return.Airlines,
                    plan.Return.FlightNumbers,
                    plan.Return.TotalDurationMinutes,
                    plan.Return.StopCount,
                    plan.Return.Price,
                    plan.Return.CurrencyCode
                },
            firstDayAvailableFrom =
                plan.FirstDayAvailableFrom?.ToString(@"hh\:mm"),
            lastDayMustEndBy =
                plan.LastDayMustEndBy?.ToString(@"hh\:mm")
        };
    }
}

internal sealed class SearchVerifiedCatalogAgentTool : IItineraryAgentTool
{
    private readonly IItineraryTravelDataService? _travelDataService;
    private readonly Trip _trip;
    private readonly IReadOnlyList<ItineraryCityStop> _cityPlan;
    private readonly GenerateItineraryPreferences _preferences;
    private readonly string _currencyCode;
    private readonly int _travelerCount;
    private readonly IReadOnlySet<string> _previouslyUsedPlaceIds;

    public SearchVerifiedCatalogAgentTool(
        IItineraryTravelDataService? travelDataService,
        Trip trip,
        IReadOnlyList<ItineraryCityStop> cityPlan,
        GenerateItineraryPreferences preferences,
        string currencyCode,
        int travelerCount,
        IReadOnlySet<string> previouslyUsedPlaceIds)
    {
        _travelDataService = travelDataService;
        _trip = trip;
        _cityPlan = cityPlan;
        _preferences = preferences;
        _currencyCode = currencyCode;
        _travelerCount = travelerCount;
        _previouslyUsedPlaceIds = previouslyUsedPlaceIds;
    }

    public ItineraryAgentToolDefinition Definition { get; } = new(
        "search_verified_catalog",
        "Load Safarni's bounded verified place catalog for every trip city. The result includes exact P references that must be used for activities, meals and accommodations.",
        """
        {
          "type": "object",
          "properties": {}
        }
        """);

    public async Task<ItineraryAgentToolExecution> ExecuteAsync(
        ItineraryAgentContext context,
        string argumentsJson,
        CancellationToken cancellationToken)
    {
        if (!context.CatalogSearchAttempted)
        {
            context.CatalogSearchAttempted = true;
            var places = await ItineraryPlaceCatalog.LoadSerpPrimaryAsync(
                _travelDataService,
                _trip,
                _cityPlan,
                _preferences,
                _currencyCode,
                _travelerCount,
                _previouslyUsedPlaceIds,
                cancellationToken);

            context.ReplaceCatalog(places);
            context.RecordAction("search_verified_catalog");
        }

        return new ItineraryAgentToolExecution(
            JsonSerializer.Serialize(new
            {
                configured = _travelDataService?.IsConfigured == true,
                totalCandidates = context.Places.Count,
                message = "Verified catalog is stored server-side. Use the compact CURRENT SERVER STATE on the next turn; do not request the catalog again unless new hotel evidence is required."
            }),
            "Agent loaded verified itinerary candidates");
    }
}

internal sealed class SearchHotelsAgentTool : IItineraryAgentTool
{
    private readonly IItineraryTravelDataService? _travelDataService;
    private readonly Trip _trip;
    private readonly IReadOnlyList<ItineraryCityStop> _cityPlan;
    private readonly GenerateItineraryPreferences _preferences;
    private readonly string _currencyCode;
    private readonly int _travelerCount;

    public SearchHotelsAgentTool(
        IItineraryTravelDataService? travelDataService,
        Trip trip,
        IReadOnlyList<ItineraryCityStop> cityPlan,
        GenerateItineraryPreferences preferences,
        string currencyCode,
        int travelerCount)
    {
        _travelDataService = travelDataService;
        _trip = trip;
        _cityPlan = cityPlan;
        _preferences = preferences;
        _currencyCode = currencyCode;
        _travelerCount = travelerCount;
    }

    public ItineraryAgentToolDefinition Definition { get; } = new(
        "search_hotels",
        "Search additional verified live hotels for one exact trip city when the current verified catalog is insufficient.",
        """
        {
          "type": "object",
          "properties": {
            "city": {
              "type": "string",
              "description": "Exact city from the trip schedule."
            }
          },
          "required": ["city"]
        }
        """);

    public async Task<ItineraryAgentToolExecution> ExecuteAsync(
        ItineraryAgentContext context,
        string argumentsJson,
        CancellationToken cancellationToken)
    {
        if (string.Equals(
                _preferences.AccommodationStyle,
                "already_booked",
                StringComparison.OrdinalIgnoreCase))
        {
            return new ItineraryAgentToolExecution(
                "{\"skipped\":true,\"reason\":\"Traveler already has accommodation.\"}",
                "Agent skipped unnecessary hotel search");
        }

        var requestedCity = AgentJson.ReadString(
            argumentsJson,
            "city");
        var cityStop = _cityPlan.FirstOrDefault(stop =>
            string.Equals(
                stop.City,
                requestedCity,
                StringComparison.OrdinalIgnoreCase));

        if (cityStop is null)
        {
            return new ItineraryAgentToolExecution(
                JsonSerializer.Serialize(new
                {
                    error = "Unknown city.",
                    allowedCities = _cityPlan
                        .Select(stop => stop.City)
                        .ToArray()
                }),
                "Agent hotel search needs a valid city");
        }

        var action = $"search_hotels:{cityStop.City}";
        if (context.HasAction(action))
        {
            return new ItineraryAgentToolExecution(
                JsonSerializer.Serialize(new
                {
                    cached = true,
                    city = cityStop.City,
                    candidates = context.Places.Count(candidate =>
                        string.Equals(
                            candidate.City,
                            cityStop.City,
                            StringComparison.OrdinalIgnoreCase) &&
                        candidate.Purpose == ItineraryPlacePurpose.Accommodation),
                    message = "Hotel evidence is already stored server-side."
                }),
                $"Agent reused hotel evidence for {cityStop.City}");
        }

        var checkOutDate = cityStop.GetCheckOutDate(_trip);
        if (checkOutDate is null ||
            _travelDataService?.IsConfigured != true)
        {
            return new ItineraryAgentToolExecution(
                JsonSerializer.Serialize(new
                {
                    city = cityStop.City,
                    candidates = 0,
                    reason = checkOutDate is null
                        ? "No hotel night is required for this city."
                        : "Travel data service is not configured."
                }),
                $"Agent checked hotels for {cityStop.City}");
        }

        var hotels = await _travelDataService.SearchHotelsAsync(
            cityStop.City,
            _preferences.AccommodationStyle,
            cityStop.GetStartDate(_trip),
            checkOutDate.Value,
            _travelerCount,
            _currencyCode,
            _preferences.PreferredLanguage,
            cancellationToken);

        var selected = hotels
            .Where(hotel =>
                !string.IsNullOrWhiteSpace(hotel.PlaceId) &&
                !string.IsNullOrWhiteSpace(hotel.Name))
            .OrderByDescending(hotel => hotel.Rating ?? 0)
            .Take(6)
            .ToList();

        context.AddHotels(
            cityStop.City,
            cityStop.NightCount,
            selected);
        context.RecordAction(action);

        return new ItineraryAgentToolExecution(
            JsonSerializer.Serialize(new
            {
                city = cityStop.City,
                candidates = selected.Count,
                message = "Additional verified hotels were stored server-side and will appear in the compact state."
            }),
            $"Agent searched additional hotels for {cityStop.City}");
    }
}

internal sealed class CalculateRouteAgentTool : IItineraryAgentTool
{
    private readonly IItineraryRouteService? _routeService;
    private readonly GenerateItineraryPreferences _preferences;

    public CalculateRouteAgentTool(
        IItineraryRouteService? routeService,
        GenerateItineraryPreferences preferences)
    {
        _routeService = routeService;
        _preferences = preferences;
    }

    public ItineraryAgentToolDefinition Definition { get; } = new(
        "calculate_route",
        "Calculate verified distance and duration between two already-loaded place references. Use this to test whether a proposed same-day sequence is realistic.",
        """
        {
          "type": "object",
          "properties": {
            "originPlaceReference": {"type": "string"},
            "destinationPlaceReference": {"type": "string"}
          },
          "required": ["originPlaceReference", "destinationPlaceReference"]
        }
        """);

    public async Task<ItineraryAgentToolExecution> ExecuteAsync(
        ItineraryAgentContext context,
        string argumentsJson,
        CancellationToken cancellationToken)
    {
        var originReference = AgentJson.ReadString(
            argumentsJson,
            "originPlaceReference");
        var destinationReference = AgentJson.ReadString(
            argumentsJson,
            "destinationPlaceReference");

        var origin = ItineraryPlaceCatalog.FindCandidate(
            context.Places,
            originReference);
        var destination = ItineraryPlaceCatalog.FindCandidate(
            context.Places,
            destinationReference);

        if (origin?.Place.Latitude is null ||
            origin.Place.Longitude is null ||
            destination?.Place.Latitude is null ||
            destination.Place.Longitude is null)
        {
            return new ItineraryAgentToolExecution(
                JsonSerializer.Serialize(new
                {
                    error = "Both references must exist in the loaded catalog and have coordinates.",
                    originPlaceReference = originReference,
                    destinationPlaceReference = destinationReference
                }),
                "Agent route check needs valid place references");
        }

        if (!string.Equals(
                origin.City,
                destination.City,
                StringComparison.OrdinalIgnoreCase))
        {
            return new ItineraryAgentToolExecution(
                JsonSerializer.Serialize(new
                {
                    error = "calculate_route is for local same-city feasibility only.",
                    originCity = origin.City,
                    destinationCity = destination.City
                }),
                "Agent rejected cross-city local route check");
        }

        if (_routeService?.IsConfigured != true)
        {
            return new ItineraryAgentToolExecution(
                "{\"configured\":false,\"message\":\"Route service is not configured.\"}",
                "Agent could not verify route timing");
        }

        var route = await _routeService.ComputeRouteAsync(
            new ItineraryRoutePoint(
                origin.Place.Latitude.Value,
                origin.Place.Longitude.Value),
            new ItineraryRoutePoint(
                destination.Place.Latitude.Value,
                destination.Place.Longitude.Value),
            _preferences.Transportation,
            _preferences.PreferredLanguage,
            cancellationToken);

        context.RouteChecksPerformed++;
        context.RecordAction(
            $"calculate_route:{origin.Reference}:{destination.Reference}");

        var routeSummary = route is null
            ? $"{origin.Reference}->{destination.Reference}: no verified route returned"
            : $"{origin.Reference}->{destination.Reference}: {route.DurationMinutes} min, {route.DistanceMeters} m, mode={route.TravelMode}";
        context.RecordRouteObservation(routeSummary);

        return new ItineraryAgentToolExecution(
            JsonSerializer.Serialize(new
            {
                origin = origin.Reference,
                destination = destination.Reference,
                distanceMeters = route?.DistanceMeters,
                durationMinutes = route?.DurationMinutes,
                travelMode = route?.TravelMode
            }),
            $"Agent checked route feasibility in {origin.City}");
    }
}


internal sealed class SetItineraryDraftAgentTool : IItineraryAgentTool
{
    public ItineraryAgentToolDefinition Definition { get; } = new(
        "set_itinerary_draft",
        "Store the complete itinerary draft once on the Safarni backend. After this, use revise_itinerary_day for targeted fixes instead of resending the entire itinerary.",
        AgentToolSchemas.CandidateItineraryParameters);

    public Task<ItineraryAgentToolExecution> ExecuteAsync(
        ItineraryAgentContext context,
        string argumentsJson,
        CancellationToken cancellationToken)
    {
        var itineraryJson = AgentJson.ReadObjectJson(
            argumentsJson,
            "itinerary");

        try
        {
            context.SetDraftJson(itineraryJson);
            context.RecordAction("set_itinerary_draft");

            return Task.FromResult(new ItineraryAgentToolExecution(
                JsonSerializer.Serialize(new
                {
                    stored = true,
                    days = context.DraftDays?.Count ?? 0,
                    instruction = "The full draft is now server-side. Use check_budget and validate_itinerary with empty arguments. If validation fails, use revise_itinerary_day for the affected day only."
                }),
                "Agent stored itinerary draft"));
        }
        catch (Exception exception)
            when (exception is JsonException or ItineraryOutputException)
        {
            return Task.FromResult(new ItineraryAgentToolExecution(
                JsonSerializer.Serialize(new
                {
                    stored = false,
                    error = exception.Message
                }),
                "Agent draft was malformed"));
        }
    }
}

internal sealed class ReviseItineraryDayAgentTool : IItineraryAgentTool
{
    public ItineraryAgentToolDefinition Definition { get; } = new(
        "revise_itinerary_day",
        "Replace one day in the server-side itinerary draft. Prefer this after validation failure so you do not resend the full itinerary.",
        AgentToolSchemas.CandidateDayParameters);

    public Task<ItineraryAgentToolExecution> ExecuteAsync(
        ItineraryAgentContext context,
        string argumentsJson,
        CancellationToken cancellationToken)
    {
        var dayJson = AgentJson.ReadObjectJson(
            argumentsJson,
            "day");

        try
        {
            context.ReplaceDraftDayJson(dayJson);
            // We intentionally derive the action from the payload without
            // echoing the full replacement day back to Gemini.
            using var document = JsonDocument.Parse(dayJson);
            var parsedDayNumber = document.RootElement.TryGetProperty(
                    "dayNumber",
                    out var numberElement) &&
                numberElement.TryGetInt32(out var value)
                    ? value
                    : 0;

            context.RecordAction($"revise_itinerary_day:{parsedDayNumber}");

            return Task.FromResult(new ItineraryAgentToolExecution(
                JsonSerializer.Serialize(new
                {
                    stored = true,
                    dayNumber = parsedDayNumber,
                    instruction = "Only this day was replaced. Re-run validate_itinerary."
                }),
                $"Agent revised itinerary day {parsedDayNumber}"));
        }
        catch (Exception exception)
            when (exception is JsonException or ItineraryOutputException)
        {
            return Task.FromResult(new ItineraryAgentToolExecution(
                JsonSerializer.Serialize(new
                {
                    stored = false,
                    error = exception.Message
                }),
                "Agent day revision was malformed"));
        }
    }
}

internal sealed class CheckBudgetAgentTool : IItineraryAgentTool
{
    private readonly decimal? _budgetAmount;
    private readonly string _currencyCode;

    public CheckBudgetAgentTool(
        decimal? budgetAmount,
        string currencyCode)
    {
        _budgetAmount = budgetAmount;
        _currencyCode = currencyCode;
    }

    public ItineraryAgentToolDefinition Definition { get; } = new(
        "check_budget",
        "Calculate deterministic known cost for the server-side itinerary draft. The full itinerary must already have been stored with set_itinerary_draft.",
        AgentToolSchemas.EmptyObjectParameters);

    public Task<ItineraryAgentToolExecution> ExecuteAsync(
        ItineraryAgentContext context,
        string argumentsJson,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(context.DraftItineraryJson))
        {
            return Task.FromResult(new ItineraryAgentToolExecution(
                "{\"error\":\"No server-side draft exists. Call set_itinerary_draft first.\"}",
                "Agent budget check needs an itinerary draft"));
        }

        decimal generatedKnownCost = 0m;
        try
        {
            using var document = JsonDocument.Parse(context.DraftItineraryJson);
            if (document.RootElement.TryGetProperty(
                    "days",
                    out var days) &&
                days.ValueKind == JsonValueKind.Array)
            {
                foreach (var day in days.EnumerateArray())
                {
                    if (!day.TryGetProperty(
                            "items",
                            out var items) ||
                        items.ValueKind != JsonValueKind.Array)
                    {
                        continue;
                    }

                    foreach (var item in items.EnumerateArray())
                    {
                        var category = item.TryGetProperty(
                            "category",
                            out var categoryElement)
                            ? categoryElement.GetString()
                            : null;
                        var placeReference = item.TryGetProperty(
                            "placeReference",
                            out var referenceElement)
                            ? referenceElement.GetString()
                            : null;

                        if (string.Equals(
                                category,
                                "accommodation",
                                StringComparison.OrdinalIgnoreCase))
                        {
                            generatedKnownCost +=
                                ItineraryPlaceCatalog.FindPlace(
                                    context.Places,
                                    placeReference)?.TotalRate ?? 0m;
                            continue;
                        }

                        if (item.TryGetProperty(
                                "estimatedCost",
                                out var costElement) &&
                            costElement.ValueKind == JsonValueKind.Number &&
                            costElement.TryGetDecimal(out var cost))
                        {
                            generatedKnownCost += cost;
                        }
                    }
                }
            }
        }
        catch (JsonException exception)
        {
            return Task.FromResult(new ItineraryAgentToolExecution(
                JsonSerializer.Serialize(new
                {
                    error = $"Stored itinerary draft is invalid JSON: {exception.Message}"
                }),
                "Agent budget check rejected malformed draft"));
        }

        var flightCost =
            (context.FlightPlan.Outbound?.Price ?? 0m) +
            (context.FlightPlan.Return?.Price ?? 0m);
        var totalKnownCost = generatedKnownCost + flightCost;
        decimal? remaining = _budgetAmount is null
            ? null
            : _budgetAmount.Value - totalKnownCost;
        var withinBudget = remaining is null || remaining >= 0;

        context.BudgetCheckPerformed = true;
        context.LastBudgetSummary =
            $"known={totalKnownCost:0.##} {_currencyCode}; remaining={(remaining is null ? "n/a" : remaining.Value.ToString("0.##"))}; withinBudget={withinBudget}";
        context.RecordAction("check_budget");

        return Task.FromResult(new ItineraryAgentToolExecution(
            JsonSerializer.Serialize(new
            {
                budget = _budgetAmount,
                currency = _currencyCode,
                totalKnownCost,
                remaining,
                withinBudget
            }),
            "Agent checked candidate budget"));
    }
}

internal sealed class ValidateItineraryAgentTool : IItineraryAgentTool
{
    private readonly Trip _trip;
    private readonly IReadOnlyList<ItineraryCityStop> _cityPlan;
    private readonly GenerateItineraryPreferences _preferences;
    private readonly decimal? _totalBudgetAmount;
    private readonly Itinerary? _previousItinerary;
    private readonly bool _requiresFlightEvidence;

    public ValidateItineraryAgentTool(
        Trip trip,
        IReadOnlyList<ItineraryCityStop> cityPlan,
        GenerateItineraryPreferences preferences,
        decimal? totalBudgetAmount,
        Itinerary? previousItinerary,
        bool requiresFlightEvidence)
    {
        _trip = trip;
        _cityPlan = cityPlan;
        _preferences = preferences;
        _totalBudgetAmount = totalBudgetAmount;
        _previousItinerary = previousItinerary;
        _requiresFlightEvidence = requiresFlightEvidence;
    }

    public ItineraryAgentToolDefinition Definition { get; } = new(
        "validate_itinerary",
        "Run Safarni's deterministic parser, repairs and business-rule validators against the current server-side draft. Do not resend the full itinerary.",
        AgentToolSchemas.EmptyObjectParameters);

    public Task<ItineraryAgentToolExecution> ExecuteAsync(
        ItineraryAgentContext context,
        string argumentsJson,
        CancellationToken cancellationToken)
    {
        var candidateJson = context.DraftItineraryJson;

        try
        {
            if (string.IsNullOrWhiteSpace(candidateJson))
            {
                throw new ItineraryOutputException(
                    "No itinerary draft exists. Call set_itinerary_draft first.");
            }
            if (!context.CatalogSearchAttempted ||
                context.Places.Count == 0)
            {
                throw new ItineraryOutputException(
                    "Verified place evidence is missing. Call search_verified_catalog before validating the itinerary.");
            }

            if (_requiresFlightEvidence && !context.FlightSearchAttempted)
            {
                throw new ItineraryOutputException(
                    "Verified flight evidence is required for this trip. Call search_flights before validating the itinerary.");
            }

            var totalTripDays = Math.Max(
                1,
                (_trip.EndDate.Date - _trip.StartDate.Date).Days + 1);
            var generatedDays = ItineraryOutputParser.ParseRange(
                candidateJson,
                _trip,
                1,
                totalTripDays)
                .OrderBy(day => day.DayNumber)
                .ToList();

            ItineraryOutputParser.NormalizeChronologicalOrder(
                generatedDays);
            ItineraryFlightWindowRepair.Repair(
                generatedDays,
                context.FlightPlan,
                totalTripDays);

            foreach (var cityStop in _cityPlan)
            {
                var cityDays = generatedDays
                    .Where(day =>
                        day.DayNumber >= cityStop.StartDayNumber &&
                        day.DayNumber <= cityStop.EndDayNumber)
                    .ToList();

                ItineraryAccommodationResolver.NormalizeCityStay(
                    cityDays,
                    cityStop,
                    _preferences,
                    context.Places,
                    context.FlightPlan,
                    totalTripDays);

                foreach (var day in cityDays)
                {
                    ItineraryPlaceCatalog.ResolvePlaceReferences(
                        day,
                        context.Places,
                        cityStop.City);
                }

                ItineraryPlaceCatalog.ReconcileActivityItems(
                    cityDays,
                    context.Places,
                    cityStop.City,
                    _preferences.PreferredLanguage);
                ItineraryDuplicatePlaceResolver.ReplaceDuplicates(
                    cityDays,
                    cityStop.City,
                    _preferences.PreferredLanguage,
                    context.Places);
                ItineraryMinimumActivityResolver.Ensure(
                    cityDays,
                    context.Places,
                    cityStop,
                    _preferences.PreferredLanguage,
                    context.FlightPlan,
                    totalTripDays);
            }

            ItineraryPlaceCatalog.ValidateRemainingOtherItems(
                generatedDays);
            ItineraryOutputParser.ValidateComplete(
                generatedDays,
                _trip);
            ItineraryOutputParser.ValidatePlaceReferences(
                generatedDays,
                context.Places,
                _cityPlan);

            var verifiedFlightCost =
                (context.FlightPlan.Outbound?.Price ?? 0m) +
                (context.FlightPlan.Return?.Price ?? 0m);
            decimal? planningBudget = _totalBudgetAmount is null
                ? null
                : _totalBudgetAmount.Value - verifiedFlightCost;

            if (planningBudget is <= 0)
            {
                throw new ItineraryOutputException(
                    "The verified flights consume the available budget. Choose a different feasible plan or report generation failure.");
            }

            ItineraryOutputParser.ValidatePlanRules(
                generatedDays,
                _preferences,
                planningBudget,
                context.Places,
                _cityPlan,
                context.FlightPlan,
                totalTripDays);
            ItineraryOutputParser.ValidateFlightConstraints(
                generatedDays,
                context.FlightPlan,
                totalTripDays);
            ValidateRegenerationDifference(
                _previousItinerary,
                generatedDays,
                context.Places);

            context.SetValidatedDays(generatedDays);
            context.RecordAction("validate_itinerary:success");

            return Task.FromResult(new ItineraryAgentToolExecution(
                JsonSerializer.Serialize(new
                {
                    valid = true,
                    days = context.ValidatedDays?.Count ?? 0,
                    message = "Deterministic validation passed. The normalized itinerary is stored server-side; call submit_itinerary without resending it."
                }),
                "Agent produced a valid itinerary"));
        }
        catch (ItineraryOutputException exception)
        {
            context.ClearValidation(exception.Message);
            context.RecordAction("validate_itinerary:rejected");

            return Task.FromResult(new ItineraryAgentToolExecution(
                JsonSerializer.Serialize(new
                {
                    valid = false,
                    error = exception.Message,
                    instruction = "Prefer revise_itinerary_day for only the affected day, then call validate_itinerary again. Use set_itinerary_draft again only when the error requires broad multi-day restructuring."
                }),
                "Agent itinerary needs revision"));
        }
    }

    private static void ValidateRegenerationDifference(
        Itinerary? previousItinerary,
        IReadOnlyList<AiItineraryDay> generatedDays,
        IReadOnlyList<ItineraryPlaceCandidate> places)
    {
        if (previousItinerary is null)
        {
            return;
        }

        var previousIds = previousItinerary.Days
            .SelectMany(day => day.Items)
            .Where(item =>
                item.Category is ItineraryItemCategory.Activity or
                    ItineraryItemCategory.Meal or
                    ItineraryItemCategory.Accommodation)
            .Select(item => item.GooglePlaceId)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

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
}

internal sealed class SubmitItineraryAgentTool : IItineraryAgentTool
{
    public ItineraryAgentToolDefinition Definition { get; } = new(
        "submit_itinerary",
        "Submit the most recently validated itinerary as the agent's final plan. This succeeds only if deterministic validation has already passed and the candidate has not been changed afterward.",
        """
        {
          "type": "object",
          "properties": {
            "reason": {
              "type": "string",
              "description": "Short reason the validated itinerary is ready."
            }
          },
          "required": ["reason"]
        }
        """);

    public Task<ItineraryAgentToolExecution> ExecuteAsync(
        ItineraryAgentContext context,
        string argumentsJson,
        CancellationToken cancellationToken)
    {
        if (context.ValidatedDays is null ||
            context.ValidatedItineraryJson is null)
        {
            return Task.FromResult(new ItineraryAgentToolExecution(
                JsonSerializer.Serialize(new
                {
                    submitted = false,
                    error = context.LastValidationError ??
                        "No validated candidate exists. Call validate_itinerary first."
                }),
                "Agent cannot submit an unvalidated itinerary"));
        }

        context.Submitted = true;
        context.RecordAction("submit_itinerary");

        return Task.FromResult(new ItineraryAgentToolExecution(
            JsonSerializer.Serialize(new
            {
                submitted = true,
                days = context.ValidatedDays.Count,
                message = "Safarni accepted the validated itinerary."
            }),
            "Agent submitted the validated itinerary"));
    }
}


internal static class AgentToolSchemas
{
    public const string EmptyObjectParameters = """
        {
          "type": "object",
          "properties": {}
        }
        """;

    public const string CandidateDayParameters = """
        {
          "type": "object",
          "properties": {
            "day": {
              "type": "object",
              "properties": {
                "dayNumber": {"type": "integer"},
                "date": {"type": "string"},
                "title": {"type": "string"},
                "notes": {"type": "string"},
                "items": {
                  "type": "array",
                  "items": {
                    "type": "object",
                    "properties": {
                      "startTime": {"type": "string"},
                      "endTime": {"type": "string"},
                      "title": {"type": "string"},
                      "description": {"type": "string"},
                      "location": {"type": "string"},
                      "placeReference": {"type": "string"},
                      "category": {"type": "string"},
                      "estimatedCost": {"type": "number"},
                      "orderIndex": {"type": "integer"}
                    },
                    "required": ["startTime", "endTime", "title", "category", "orderIndex"]
                  }
                }
              },
              "required": ["dayNumber", "date", "title", "items"]
            }
          },
          "required": ["day"]
        }
        """;

    public const string CandidateItineraryParameters = """
        {
          "type": "object",
          "properties": {
            "itinerary": {
              "type": "object",
              "properties": {
                "days": {
                  "type": "array",
                  "items": {
                    "type": "object",
                    "properties": {
                      "dayNumber": {"type": "integer"},
                      "date": {"type": "string"},
                      "title": {"type": "string"},
                      "notes": {"type": "string"},
                      "items": {
                        "type": "array",
                        "items": {
                          "type": "object",
                          "properties": {
                            "startTime": {"type": "string"},
                            "endTime": {"type": "string"},
                            "title": {"type": "string"},
                            "description": {"type": "string"},
                            "location": {"type": "string"},
                            "placeReference": {"type": "string"},
                            "category": {"type": "string"},
                            "estimatedCost": {"type": "number"},
                            "orderIndex": {"type": "integer"}
                          },
                          "required": ["startTime", "endTime", "title", "category", "orderIndex"]
                        }
                      }
                    },
                    "required": ["dayNumber", "date", "title", "items"]
                  }
                }
              },
              "required": ["days"]
            }
          },
          "required": ["itinerary"]
        }
        """;
}

internal static class AgentJson
{
    public static string? ReadString(
        string argumentsJson,
        string propertyName)
    {
        try
        {
            using var document = JsonDocument.Parse(
                string.IsNullOrWhiteSpace(argumentsJson)
                    ? "{}"
                    : argumentsJson);
            return document.RootElement.TryGetProperty(
                    propertyName,
                    out var value) &&
                value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static string ReadObjectJson(
        string argumentsJson,
        string propertyName)
    {
        try
        {
            using var document = JsonDocument.Parse(
                string.IsNullOrWhiteSpace(argumentsJson)
                    ? "{}"
                    : argumentsJson);

            if (!document.RootElement.TryGetProperty(
                    propertyName,
                    out var value) ||
                value.ValueKind != JsonValueKind.Object)
            {
                return "{}";
            }

            return value.GetRawText();
        }
        catch (JsonException)
        {
            return "{}";
        }
    }
}
