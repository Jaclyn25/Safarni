using System.Text.Json;

namespace Safarni.Application.Features.Trips.GenerateItinerary;

/// <summary>
/// Guided traveler choices used to generate a structured itinerary.
/// Free-form notes are optional and supplement the guided selections.
/// </summary>
public sealed record GenerateItineraryPreferences
{
    public const int CurrentQualityVersion = 2;

    public int QualityVersion { get; init; }

    public IReadOnlyList<string> Interests { get; init; } = [];

    public string Pace { get; init; } = string.Empty;

    public IReadOnlyList<string> Transportation { get; init; } = [];

    public string AccommodationStyle { get; init; } = string.Empty;

    public IReadOnlyList<string> DietaryNeeds { get; init; } = [];

    public IReadOnlyList<string> AccessibilityNeeds { get; init; } = [];

    public string PreferredLanguage { get; init; } = "en";

    public string? AdditionalNotes { get; init; }
}

internal static class GenerateItineraryPreferencesSerializer
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public static string Serialize(
        GenerateItineraryPreferences preferences)
    {
        return JsonSerializer.Serialize(
            preferences,
            JsonOptions);
    }

    public static GenerateItineraryPreferences Deserialize(
        string storedPreferences)
    {
        if (!string.IsNullOrWhiteSpace(storedPreferences) &&
            storedPreferences.TrimStart().StartsWith('{'))
        {
            try
            {
                var preferences =
                    JsonSerializer.Deserialize<GenerateItineraryPreferences>(
                        storedPreferences,
                        JsonOptions);

                if (preferences is not null)
                {
                    return preferences with
                    {
                        Interests = preferences.Interests ?? [],
                        Transportation =
                            preferences.Transportation ?? [],
                        DietaryNeeds =
                            preferences.DietaryNeeds ?? [],
                        AccessibilityNeeds =
                            preferences.AccessibilityNeeds ?? []
                    };
                }
            }
            catch (JsonException)
            {
                // Fall back to the legacy free-text representation.
            }
        }

        return CreateLegacyPreferences(storedPreferences);
    }

    private static GenerateItineraryPreferences
        CreateLegacyPreferences(string storedPreferences)
    {
        var notes = string.IsNullOrWhiteSpace(storedPreferences)
            ? null
            : storedPreferences.Trim();

        return new GenerateItineraryPreferences
        {
            Interests = ["culture"],
            Pace = "balanced",
            Transportation = ["mixed"],
            AccommodationStyle = "mid_range",
            PreferredLanguage =
                ContainsArabic(notes) ? "ar" : "en",
            AdditionalNotes = notes
        };
    }

    private static bool ContainsArabic(string? value)
    {
        return value?.Any(character =>
            character is >= '\u0600' and <= '\u06FF'
            or >= '\u0750' and <= '\u077F'
            or >= '\u08A0' and <= '\u08FF'
            or >= '\uFB50' and <= '\uFDFF'
            or >= '\uFE70' and <= '\uFEFF') == true;
    }
}
