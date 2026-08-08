using System.Text;
using Safarni.Application.Features.Trips.GenerateItinerary;
using Safarni.Domain.Entities;

namespace Safarni.Application.Features.Trips.GenerateItinerary.Agent;

internal static class ItineraryAgentPromptBuilder
{
    // Keep stable instructions short and at the beginning of every request.
    // Gemini can cache this prefix more effectively than a frequently changing
    // prompt containing catalogs or full itinerary JSON.
    public const string SystemPrompt = """
        You are Safarni's bounded agentic itinerary planner.

        Decide which approved tool to call next. The backend stores authoritative
        state (catalog, flights, draft, validation result) so do not repeat large
        data that is already stored server-side.

        Core workflow rules:
        - Never invent live flights, hotels, named places, prices, routes or availability.
        - Load verified evidence with search_verified_catalog and search_flights when needed.
        - Build the complete itinerary once, then call set_itinerary_draft.
        - After a draft is stored, call check_budget and validate_itinerary with empty arguments.
        - If validation fails, prefer revise_itinerary_day and send ONLY the affected day.
          Use set_itinerary_draft again only for a broad multi-day restructure.
        - Use calculate_route only when route feasibility is genuinely uncertain.
        - After validate_itinerary returns valid=true, call submit_itinerary without resending the plan.
        - Do not finish with prose; the successful final action is submit_itinerary.
        - Stay within the bounded tool/step budget.

        Planning rules:
        - Categories: activity, meal, transport, accommodation, other.
        - activity/meal/accommodation must use exact verified P references from CURRENT SERVER STATE.
        - Stay inside the city assigned to each day.
        - Respect first-day arrival and last-day departure windows when verified flights exist.
        - Use chronological, non-overlapping HH:mm times.
        - Do not create local transport items between venues; Safarni enriches routes after acceptance.
        - Avoid duplicate attractions and repetitive meals.
        - If accommodation is already booked, do not add a hotel.
        - Accommodation estimatedCost should be null; backend attaches verified rates.
        - Other estimated costs are totals for all travelers.
        - Respect the supplied budget and requested language.
        - Keep descriptions concise.
        """;

    public static string BuildGoal(
        Trip trip,
        IReadOnlyList<ItineraryCityStop> cityPlan,
        GenerateItineraryPreferences preferences,
        decimal? budgetAmount,
        string currencyCode,
        int travelerCount,
        bool isRegeneration,
        IReadOnlyCollection<string> previousPlaceNames)
    {
        var builder = new StringBuilder();
        var totalDays = Math.Max(
            1,
            (trip.EndDate.Date - trip.StartDate.Date).Days + 1);

        builder.AppendLine("GOAL: create, validate and submit the final Safarni itinerary.");
        builder.AppendLine($"trip={trip.Id}; dates={trip.StartDate:yyyy-MM-dd}..{trip.EndDate:yyyy-MM-dd}; days={totalDays}; destination={trip.Destination}");
        builder.AppendLine($"origin={trip.OriginCity ?? "-"},{trip.OriginCountry}; originAirport={trip.OriginAirportCode ?? "-"}; travelers={travelerCount}");
        builder.AppendLine(
            budgetAmount is null
                ? $"budget=none; currency={currencyCode}"
                : $"budget={budgetAmount.Value:0.##} {currencyCode}");
        builder.AppendLine($"language={preferences.PreferredLanguage}; pace={preferences.Pace}; accommodation={preferences.AccommodationStyle}");
        builder.AppendLine($"interests={Format(preferences.Interests, 12)}");
        builder.AppendLine($"transport={Format(preferences.Transportation, 8)}");

        if (!string.IsNullOrWhiteSpace(preferences.AdditionalNotes))
        {
            builder.AppendLine($"notes={Clean(preferences.AdditionalNotes, 350)}");
        }

        builder.AppendLine("CITY SCHEDULE:");
        foreach (var stop in cityPlan)
        {
            builder.AppendLine(
                $"D{stop.StartDayNumber}-D{stop.EndDayNumber}:{stop.City}; airport={stop.AirportCode ?? "-"}; nights={stop.NightCount}");
        }

        if (isRegeneration)
        {
            builder.AppendLine("REGENERATION: prefer unused verified places when alternatives exist.");
            builder.AppendLine($"previousPlaces={Format(previousPlaceNames, 30)}");
        }

        builder.AppendLine("Start by deciding which evidence is missing. Do not request data already listed in CURRENT SERVER STATE.");
        return builder.ToString();
    }

    private static string Format(
        IEnumerable<string> values,
        int maxItems)
    {
        var normalized = values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(maxItems)
            .ToList();

        return normalized.Count == 0
            ? "none"
            : string.Join(",", normalized);
    }

    private static string Clean(string value, int maxLength)
    {
        var normalized = value
            .Replace('\r', ' ')
            .Replace('\n', ' ')
            .Trim();
        return normalized.Length <= maxLength
            ? normalized
            : normalized[..maxLength];
    }
}
