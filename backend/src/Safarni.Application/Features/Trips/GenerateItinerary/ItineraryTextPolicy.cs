using ChatBotApi.Application.Common.Models.GooglePlaces;

namespace Safarni.Application.Features.Trips.GenerateItinerary;

/// <summary>
/// Keeps deterministic text introduced by the backend in the requested
/// language while preserving the exact provider name in the location field.
/// </summary>
internal static class ItineraryTextPolicy
{
    public static bool IsArabicLanguage(string? languageCode) =>
        string.Equals(
            languageCode,
            "ar",
            StringComparison.OrdinalIgnoreCase);

    public static bool ContainsArabic(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        return text.Any(character =>
            character is >= '\u0600' and <= '\u06FF'
            or >= '\u0750' and <= '\u077F'
            or >= '\u08A0' and <= '\u08FF'
            or >= '\uFB50' and <= '\uFDFF'
            or >= '\uFE70' and <= '\uFEFF');
    }

    public static string BuildActivityTitle(
        PlaceDetails place,
        string languageCode)
    {
        var name = place.Name.Trim();

        if (IsArabicLanguage(languageCode))
        {
            return ContainsArabic(name)
                ? name
                : "زيارة نشاط موثّق";
        }

        return ContainsArabic(name)
            ? "Visit verified activity"
            : name;
    }

    public static string BuildActivityDescription(
        string languageCode)
    {
        return IsArabicLanguage(languageCode)
            ? "زيارة نشاط موثّق يناسب تفضيلات الرحلة."
            : "Visit this verified activity selected for your trip.";
    }

    public static string BuildAccommodationTitle(
        PlaceDetails place,
        string languageCode)
    {
        var name = place.Name.Trim();

        if (IsArabicLanguage(languageCode))
        {
            return ContainsArabic(name)
                ? name
                : "إقامة مقترحة";
        }

        return ContainsArabic(name)
            ? "Recommended accommodation"
            : name;
    }

    public static string BuildAccommodationDescription(
        string languageCode)
    {
        return IsArabicLanguage(languageCode)
            ? "إقامة مقترحة لهذه المدينة."
            : "Recommended stay for this city.";
    }

    public static string BuildDuplicateReplacementDescription(
        bool isMeal,
        string languageCode)
    {
        if (IsArabicLanguage(languageCode))
        {
            return isMeal
                ? "توقف لتناول وجبة في مكان موثّق قريب ضمن برنامج اليوم."
                : "زيارة مكان موثّق ضمن برنامج اليوم.";
        }

        return isMeal
            ? "Enjoy a meal at this verified nearby venue."
            : "Visit this verified place as part of today's plan.";
    }
}
