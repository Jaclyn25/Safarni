using System.Globalization;
using System.Text;
using System.Text.Json;
using ChatBotApi.Application.Common.Models.GooglePlaces;
using Safarni.Application.Abstractions.Travel;
using Safarni.Application.Features.Trips.GenerateItinerary;

namespace Safarni.Application.Features.Trips.GenerateItinerary.Agent;

internal sealed record ItineraryAgentResult(
    ItineraryFlightPlan FlightPlan,
    IReadOnlyList<ItineraryPlaceCandidate> Places,
    IReadOnlyList<AiItineraryDay> GeneratedDays,
    IReadOnlyList<string> Actions);

internal sealed class ItineraryAgentContext
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

    private readonly List<ItineraryPlaceCandidate> _places = [];
    private readonly List<string> _actions = [];
    private readonly List<string> _routeObservations = [];

    public ItineraryFlightPlan FlightPlan { get; set; } = new(null, null);

    public bool FlightSearchAttempted { get; set; }

    public bool CatalogSearchAttempted { get; set; }

    public bool BudgetCheckPerformed { get; set; }

    public int RouteChecksPerformed { get; set; }

    public string? LastValidationError { get; set; }

    public string? LastBudgetSummary { get; set; }

    public IReadOnlyList<AiItineraryDay>? DraftDays { get; private set; }

    public string? DraftItineraryJson { get; private set; }

    public IReadOnlyList<AiItineraryDay>? ValidatedDays { get; private set; }

    public string? ValidatedItineraryJson { get; private set; }

    public bool Submitted { get; set; }

    public IReadOnlyList<string> Actions => _actions;

    public IReadOnlyList<ItineraryPlaceCandidate> Places =>
        _places
            .GroupBy(
                candidate =>
                    $"{candidate.City}:{candidate.Purpose}:{candidate.Place.PlaceId}",
                StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Select((candidate, index) =>
                candidate with { Reference = $"P{index + 1}" })
            .ToList();

    public void ReplaceCatalog(
        IReadOnlyList<ItineraryPlaceCandidate> places)
    {
        _places.Clear();
        _places.AddRange(places);
        ClearValidation();
    }

    public void AddHotels(
        string city,
        int? stayNights,
        IEnumerable<PlaceDetails> hotels)
    {
        foreach (var hotel in hotels)
        {
            if (string.IsNullOrWhiteSpace(hotel.PlaceId) ||
                string.IsNullOrWhiteSpace(hotel.Name))
            {
                continue;
            }

            _places.Add(new ItineraryPlaceCandidate(
                "TEMP",
                hotel,
                ItineraryPlacePurpose.Accommodation,
                city,
                stayNights));
        }

        ClearValidation();
    }

    public void SetDraftJson(string itineraryJson)
    {
        var envelope = JsonSerializer.Deserialize<AiItineraryEnvelope>(
            itineraryJson,
            JsonOptions)
            ?? throw new ItineraryOutputException(
                "The itinerary draft is empty or malformed.");

        if (envelope.Days is null || envelope.Days.Count == 0)
        {
            throw new ItineraryOutputException(
                "The itinerary draft contains no days.");
        }

        SetDraftDays(envelope.Days);
    }

    public void SetDraftDays(
        IReadOnlyList<AiItineraryDay> days)
    {
        DraftDays = days
            .OrderBy(day => day.DayNumber)
            .ToList();
        DraftItineraryJson = SerializeDays(DraftDays);
        ClearValidation();
    }

    public void ReplaceDraftDayJson(string dayJson)
    {
        var replacement = JsonSerializer.Deserialize<AiItineraryDay>(
            dayJson,
            JsonOptions)
            ?? throw new ItineraryOutputException(
                "The replacement day is empty or malformed.");

        ReplaceDraftDay(replacement);
    }

    public void ReplaceDraftDay(AiItineraryDay replacement)
    {
        if (DraftDays is null)
        {
            throw new ItineraryOutputException(
                "No itinerary draft exists. Call set_itinerary_draft first.");
        }

        var updated = DraftDays
            .Select(day => day.DayNumber == replacement.DayNumber
                ? replacement
                : day)
            .ToList();

        if (!updated.Any(day => day.DayNumber == replacement.DayNumber))
        {
            throw new ItineraryOutputException(
                $"Day {replacement.DayNumber} does not exist in the current itinerary draft.");
        }

        DraftDays = updated
            .OrderBy(day => day.DayNumber)
            .ToList();
        DraftItineraryJson = SerializeDays(DraftDays);
        ClearValidation();
    }

    public void SetValidatedDays(
        IReadOnlyList<AiItineraryDay> days)
    {
        ValidatedDays = days
            .OrderBy(day => day.DayNumber)
            .ToList();
        ValidatedItineraryJson = SerializeDays(ValidatedDays);

        // Keep the server-side draft synchronized with any safe deterministic
        // normalization/repair performed by the validators. The model therefore
        // never needs the entire normalized itinerary echoed back in a tool result.
        DraftDays = ValidatedDays;
        DraftItineraryJson = ValidatedItineraryJson;
        LastValidationError = null;
    }

    public void ClearValidation(string? error = null)
    {
        ValidatedDays = null;
        ValidatedItineraryJson = null;
        LastValidationError = error;
        Submitted = false;
    }

    public void RecordRouteObservation(string observation)
    {
        if (string.IsNullOrWhiteSpace(observation))
        {
            return;
        }

        _routeObservations.Add(observation.Trim());
        while (_routeObservations.Count > 4)
        {
            _routeObservations.RemoveAt(0);
        }
    }

    public string BuildCompactStateSummary()
    {
        var builder = new StringBuilder();
        builder.AppendLine("CURRENT SERVER STATE (authoritative; do not ask for data already present):");
        builder.AppendLine($"catalogLoaded={CatalogSearchAttempted}; candidates={Places.Count}; flightSearched={FlightSearchAttempted}; draftStored={DraftDays is not null}; validated={ValidatedDays is not null}; submitted={Submitted}");

        if (FlightSearchAttempted)
        {
            builder.AppendLine(BuildCompactFlightSummary());
        }

        if (CatalogSearchAttempted && Places.Count > 0)
        {
            builder.AppendLine("VERIFIED CANDIDATES (compact):");
            foreach (var candidate in Places)
            {
                var place = candidate.Place;
                var rating = place.Rating?.ToString("0.0", CultureInfo.InvariantCulture) ?? "-";
                var price = candidate.Purpose == ItineraryPlacePurpose.Accommodation
                    ? place.TotalRate is not null
                        ? $"total={place.TotalRate:0.##}{place.PriceCurrency}"
                        : place.RatePerNight is not null
                            ? $"night={place.RatePerNight:0.##}{place.PriceCurrency}"
                            : "price=-"
                    : string.IsNullOrWhiteSpace(place.PriceLevel)
                        ? "price=-"
                        : $"price={place.PriceLevel}";

                builder.Append(candidate.Reference)
                    .Append('|').Append(candidate.City)
                    .Append('|').Append(FormatPurpose(candidate.Purpose))
                    .Append('|').Append(Clean(place.Name, 70))
                    .Append("|r=").Append(rating)
                    .Append('|').Append(price)
                    .AppendLine();
            }
        }

        if (DraftDays is not null)
        {
            builder.AppendLine("CURRENT DRAFT SUMMARY (full draft is stored server-side):");
            foreach (var day in DraftDays.OrderBy(day => day.DayNumber))
            {
                builder.Append("D")
                    .Append(day.DayNumber)
                    .Append('|').Append(day.Date)
                    .Append('|').Append(Clean(day.Title, 50))
                    .Append(':');

                foreach (var item in day.Items.OrderBy(item => item.OrderIndex))
                {
                    builder.Append(' ')
                        .Append(item.StartTime ?? "-")
                        .Append('-').Append(item.EndTime ?? "-")
                        .Append(' ')
                        .Append(Clean(item.Title, 45))
                        .Append('[').Append(item.PlaceReference ?? "-")
                        .Append('/').Append(item.Category)
                        .Append(']');
                }

                builder.AppendLine();
            }
        }

        if (!string.IsNullOrWhiteSpace(LastBudgetSummary))
        {
            builder.AppendLine($"BUDGET: {LastBudgetSummary}");
        }

        if (!string.IsNullOrWhiteSpace(LastValidationError))
        {
            builder.AppendLine($"LAST VALIDATION ERROR: {Clean(LastValidationError, 500)}");
        }

        if (_routeObservations.Count > 0)
        {
            builder.AppendLine("RECENT ROUTE CHECKS:");
            foreach (var route in _routeObservations)
            {
                builder.AppendLine($"- {Clean(route, 220)}");
            }
        }

        if (_actions.Count > 0)
        {
            builder.AppendLine($"RECENT ACTIONS: {string.Join(", ", _actions.TakeLast(8))}");
        }

        return builder.ToString();
    }

    public bool HasAction(string action) =>
        _actions.Contains(
            action,
            StringComparer.OrdinalIgnoreCase);

    public void RecordAction(string action)
    {
        if (!string.IsNullOrWhiteSpace(action))
        {
            _actions.Add(action.Trim());
        }
    }

    private string BuildCompactFlightSummary()
    {
        static string Leg(FlightRecommendation? option)
        {
            if (option is null)
            {
                return "none";
            }

            return $"{option.DepartureAirportCode}-{option.ArrivalAirportCode} {Clean(option.DepartureTime, 25)}->{Clean(option.ArrivalTime, 25)} price={option.Price:0.##}{option.CurrencyCode}";
        }

        return $"FLIGHTS: outbound={Leg(FlightPlan.Outbound)}; return={Leg(FlightPlan.Return)}; firstDayFrom={FlightPlan.FirstDayAvailableFrom?.ToString(@"hh\:mm") ?? "-"}; lastDayBy={FlightPlan.LastDayMustEndBy?.ToString(@"hh\:mm") ?? "-"}";
    }

    private static string SerializeDays(
        IReadOnlyList<AiItineraryDay> days)
    {
        return JsonSerializer.Serialize(
            new AiItineraryEnvelope
            {
                Days = days.ToList()
            },
            JsonOptions);
    }

    private static string FormatPurpose(ItineraryPlacePurpose purpose) =>
        purpose switch
        {
            ItineraryPlacePurpose.Activity => "activity",
            ItineraryPlacePurpose.Meal => "meal",
            ItineraryPlacePurpose.Accommodation => "hotel",
            _ => purpose.ToString().ToLowerInvariant()
        };

    private static string Clean(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "-";
        }

        var normalized = value
            .Replace('\r', ' ')
            .Replace('\n', ' ')
            .Replace('|', '/')
            .Trim();

        return normalized.Length <= maxLength
            ? normalized
            : normalized[..maxLength];
    }
}

internal sealed record ItineraryAgentToolExecution(
    string ResultJson,
    string Stage);
