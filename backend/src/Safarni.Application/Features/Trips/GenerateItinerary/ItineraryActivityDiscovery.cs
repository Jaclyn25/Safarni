using System.Text.Json;

namespace Safarni.Application.Features.Trips.GenerateItinerary;

internal sealed record ItineraryActivitySuggestion(
    string Name,
    string City,
    string Kind);

internal sealed record ItineraryVerifiedActivity(
    ChatBotApi.Application.Common.Models.GooglePlaces.PlaceDetails Place,
    string City,
    string SuggestedName,
    double MatchScore);

internal static class ItineraryActivityDiscovery
{
    public const string SystemPrompt = """
        You suggest real travel activities that can be verified by a places API.

        Return newline-delimited JSON only.
        Return one JSON object per suggestion.
        Do not return markdown, an array, an introduction or explanations.

        Each line must use this exact shape:
        {"name":"Exact searchable place or provider name","city":"Exact supplied city","kind":"museum"}

        Rules:
        - Suggest only real, specific and searchable places or activity providers.
        - Match the supplied interests, pace and notes.
        - Prefer destination-specific attractions, landmarks, museums, parks,
          beaches, markets, cultural venues and named experience providers.
        - Do not suggest hotels, restaurants, cafes, meals or transportation.
        - Do not use generic phrases or category labels such as "explore the city",
          "local tour", "city center", "free time", "visit a museum",
          "Adventure Park", "Water Park", "Theme Park", "Museum",
          "Beach" or "Market". Return the official proper place name.
        - Do not invent addresses, ratings, prices, opening hours or identifiers.
        - Use each supplied city name exactly as written.
        - Keep name under 100 characters and kind under 30 characters.
        """;

    public static string BuildUserMessage(
        IReadOnlyList<ItineraryCityStop> cityPlan,
        GenerateItineraryPreferences preferences)
    {
        var cityRequests = cityPlan.Select(cityStop =>
        {
            var count = GetSuggestionCount(
                cityStop.DayCount,
                preferences.Pace);

            return $"- {cityStop.City}: {count} suggestions for {cityStop.DayCount} day(s)";
        });

        var interests = FormatValues(preferences.Interests);
        var notes = string.IsNullOrWhiteSpace(
            preferences.AdditionalNotes)
                ? "None"
                : preferences.AdditionalNotes.Trim();

        return $"""
            Suggest activity candidates for this trip.

            Cities and required suggestion counts:
            {string.Join(Environment.NewLine, cityRequests)}

            Traveler interests: {interests}
            Pace: {FormatValue(preferences.Pace)}
            Additional notes: {notes}

            Return the requested number of specific, real and searchable
            activity candidates for every city now.
            """;
    }

    public static IReadOnlyList<ItineraryActivitySuggestion> Parse(
        string output,
        IReadOnlyList<ItineraryCityStop> cityPlan,
        GenerateItineraryPreferences preferences)
    {
        var allowedCities = cityPlan
            .Select(stop => stop.City)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var parsed = ExtractJsonObjects(output)
            .Select(ParseSuggestion)
            .Select(suggestion =>
            {
                var exactCity = allowedCities.FirstOrDefault(city =>
                    string.Equals(
                        city,
                        suggestion.City,
                        StringComparison.OrdinalIgnoreCase));

                return exactCity is null
                    ? null
                    : suggestion with
                    {
                        Name = suggestion.Name.Trim(),
                        City = exactCity,
                        Kind = suggestion.Kind.Trim()
                    };
            })
            .Where(suggestion => suggestion is not null)
            .Cast<ItineraryActivitySuggestion>()
            .Where(suggestion =>
                !string.IsNullOrWhiteSpace(suggestion.Name) &&
                suggestion.Name.Length <= 100 &&
                !IsGenericSuggestion(suggestion.Name))
            .GroupBy(
                suggestion =>
                    $"{suggestion.City}:{Normalize(suggestion.Name)}",
                StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();

        var limited = new List<ItineraryActivitySuggestion>();

        foreach (var cityStop in cityPlan)
        {
            var limit = GetSuggestionCount(
                cityStop.DayCount,
                preferences.Pace);

            limited.AddRange(parsed
                .Where(suggestion =>
                    string.Equals(
                        suggestion.City,
                        cityStop.City,
                        StringComparison.OrdinalIgnoreCase))
                .Take(limit));
        }

        if (limited.Count == 0)
        {
            throw new ItineraryOutputException(
                "The AI did not return any usable activity suggestions.");
        }

        return limited;
    }

    public static int GetRecommendedMaxTokens(
        IReadOnlyList<ItineraryCityStop> cityPlan,
        string pace)
    {
        var suggestionCount = cityPlan.Sum(stop =>
            GetSuggestionCount(stop.DayCount, pace));

        return Math.Clamp(
            220 + (suggestionCount * 45),
            500,
            1300);
    }

    private static ItineraryActivitySuggestion ParseSuggestion(
        string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            var name = ReadString(root, "name");
            var city = ReadString(root, "city");
            var kind = ReadString(root, "kind");

            if (string.IsNullOrWhiteSpace(name) ||
                string.IsNullOrWhiteSpace(city))
            {
                throw new ItineraryOutputException(
                    "An activity suggestion is missing its name or city.");
            }

            return new ItineraryActivitySuggestion(
                name,
                city,
                string.IsNullOrWhiteSpace(kind)
                    ? "activity"
                    : kind);
        }
        catch (JsonException exception)
        {
            throw new ItineraryOutputException(
                $"The AI returned invalid activity JSON: {exception.Message}");
        }
    }

    private static string ReadString(
        JsonElement element,
        string propertyName)
    {
        return element.TryGetProperty(
                   propertyName,
                   out var property) &&
               property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? string.Empty
            : string.Empty;
    }

    private static int GetSuggestionCount(
        int dayCount,
        string pace)
    {
        var activitiesPerDay = pace
            .Trim()
            .ToLowerInvariant() switch
            {
                "active" => 3,
                "relaxed" => 2,
                _ => 2
            };

        return Math.Clamp(
            (Math.Max(1, dayCount) * activitiesPerDay) + 2,
            4,
            12);
    }

    private static bool IsGenericSuggestion(string value)
    {
        var normalized = Normalize(value);

        var genericSuggestions = new HashSet<string>(
        [
            "explore the city",
            "explore city",
            "city tour",
            "local tour",
            "visit a museum",
            "visit a park",
            "city center",
            "free time",
            "walking tour",
            "adventure park",
            "water park",
            "theme park",
            "museum",
            "beach",
            "market",
            "park",
            "aquarium",
            "zoo"
        ],
        StringComparer.OrdinalIgnoreCase);

        return genericSuggestions.Contains(normalized);
    }

    private static string Normalize(string value)
    {
        var characters = value
            .ToLowerInvariant()
            .Where(character =>
                char.IsLetterOrDigit(character) ||
                char.IsWhiteSpace(character))
            .ToArray();

        return string.Join(
            " ",
            new string(characters)
                .Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries));
    }

    private static string FormatValues(
        IEnumerable<string> values)
    {
        var formatted = values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(FormatValue)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return formatted.Count == 0
            ? "None"
            : string.Join(", ", formatted);
    }

    private static string FormatValue(string value)
    {
        return value
            .Trim()
            .Replace('_', ' ');
    }

    private static IReadOnlyList<string> ExtractJsonObjects(
        string output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            throw new ItineraryOutputException(
                "The AI returned an empty activity response.");
        }

        var jsonObjects = new List<string>();
        var objectStart = -1;
        var objectDepth = 0;
        var insideString = false;
        var escaped = false;

        for (var index = 0; index < output.Length; index++)
        {
            var character = output[index];

            if (objectStart < 0)
            {
                if (character == '{')
                {
                    objectStart = index;
                    objectDepth = 1;
                    insideString = false;
                    escaped = false;
                }

                continue;
            }

            if (insideString)
            {
                if (escaped)
                {
                    escaped = false;
                    continue;
                }

                if (character == '\\')
                {
                    escaped = true;
                    continue;
                }

                if (character == '"')
                {
                    insideString = false;
                }

                continue;
            }

            if (character == '"')
            {
                insideString = true;
                continue;
            }

            if (character == '{')
            {
                objectDepth++;
                continue;
            }

            if (character != '}')
            {
                continue;
            }

            objectDepth--;

            if (objectDepth != 0)
            {
                continue;
            }

            jsonObjects.Add(output[objectStart..(index + 1)]);
            objectStart = -1;
        }

        if (objectStart >= 0)
        {
            throw new ItineraryOutputException(
                "The AI activity response ended inside an incomplete JSON object.");
        }

        if (jsonObjects.Count == 0)
        {
            throw new ItineraryOutputException(
                "The AI activity response did not contain JSON objects.");
        }

        return jsonObjects;
    }
}