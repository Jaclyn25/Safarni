using System.Text.RegularExpressions;
using Safarni.Domain.Entities;

namespace Safarni.Application.Features.Trips.GenerateItinerary;

/// <summary>
/// Repairs wrong-script explanatory text deterministically. Exact provider
/// names remain untouched in location; only planner-authored visible text is
/// replaced. This keeps one malformed AI field from failing the whole trip.
/// </summary>
internal static class ItineraryLanguageNormalizer
{
    private static readonly HashSet<string> EnglishPlanningWords = new(
    [
        "arrival",
        "departure",
        "day",
        "morning",
        "afternoon",
        "evening",
        "breakfast",
        "lunch",
        "dinner",
        "transport",
        "transfer",
        "checkin",
        "checkout"
    ], StringComparer.OrdinalIgnoreCase);

    public static int Normalize(
        IReadOnlyList<AiItineraryDay> days,
        string languageCode)
    {
        var isArabic =
            ItineraryTextPolicy.IsArabicLanguage(languageCode);
        var adjustmentCount = 0;

        foreach (var day in days)
        {
            if (NeedsRepair(day.Title, isArabic))
            {
                day.Title = isArabic
                    ? $"برنامج اليوم {day.DayNumber}"
                    : $"Day {day.DayNumber} itinerary";
                adjustmentCount++;
            }

            if (NeedsRepair(day.Notes, isArabic))
            {
                day.Notes = null;
                adjustmentCount++;
            }

            foreach (var item in day.Items)
            {
                var category = ItineraryItemInput.ParseCategory(
                    item.Category);

                if (NeedsRepair(item.Title, isArabic))
                {
                    item.Title = BuildTitle(
                        category,
                        item.StartTime,
                        day.DayNumber,
                        isArabic);
                    adjustmentCount++;
                }

                if (NeedsRepair(item.Description, isArabic))
                {
                    item.Description = BuildDescription(
                        category,
                        isArabic);
                    adjustmentCount++;
                }
            }
        }

        return adjustmentCount;
    }

    internal static bool NeedsRepair(
        string? text,
        bool isArabic)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        if (!isArabic)
        {
            return ItineraryTextPolicy.ContainsArabic(text);
        }

        if (text.Any(char.IsLetter) &&
            !ItineraryTextPolicy.ContainsArabic(text))
        {
            return true;
        }

        var latinWords = Regex
            .Matches(text, @"[A-Za-z]+")
            .Select(match => match.Value.ToLowerInvariant());

        return latinWords.Any(EnglishPlanningWords.Contains);
    }

    private static string BuildTitle(
        ItineraryItemCategory category,
        string? startTime,
        int dayNumber,
        bool isArabic)
    {
        return category switch
        {
            ItineraryItemCategory.Activity => isArabic
                ? "زيارة نشاط موثّق"
                : "Visit verified activity",
            ItineraryItemCategory.Accommodation => isArabic
                ? "إقامة مقترحة"
                : "Recommended accommodation",
            ItineraryItemCategory.Meal => BuildMealTitle(
                startTime,
                dayNumber,
                isArabic),
            ItineraryItemCategory.Transport => isArabic
                ? "الانتقال بين المدن"
                : "Travel between cities",
            _ => isArabic
                ? "وقت للراحة والاستعداد"
                : "Rest and preparation"
        };
    }

    private static string BuildDescription(
        ItineraryItemCategory category,
        bool isArabic)
    {
        return category switch
        {
            ItineraryItemCategory.Activity => isArabic
                ? "زيارة نشاط موثّق يناسب تفضيلات الرحلة."
                : "Visit this verified activity selected for your trip.",
            ItineraryItemCategory.Accommodation => isArabic
                ? "إقامة مقترحة لهذه المدينة."
                : "Recommended stay for this city.",
            ItineraryItemCategory.Meal => isArabic
                ? "تجربة طبق محلي خلال هذه الوجبة."
                : "Try a local specialty during this meal.",
            ItineraryItemCategory.Transport => isArabic
                ? "وقت مخصص للانتقال الواقعي بين المدن."
                : "Reserved time for realistic intercity travel.",
            _ => isArabic
                ? "وقت مرن للراحة والاستعداد."
                : "Flexible time for rest and preparation."
        };
    }

    private static string BuildMealTitle(
        string? startTime,
        int dayNumber,
        bool isArabic)
    {
        var time = ItineraryItemInput.ParseTime(startTime) ??
                   TimeSpan.FromHours(12);

        if (time < TimeSpan.FromHours(11))
        {
            return isArabic
                ? $"إفطار — طبق محلي لليوم {dayNumber}"
                : $"Breakfast — local specialty for day {dayNumber}";
        }

        if (time >= TimeSpan.FromHours(17))
        {
            return isArabic
                ? $"عشاء — طبق محلي لليوم {dayNumber}"
                : $"Dinner — local specialty for day {dayNumber}";
        }

        return isArabic
            ? $"غداء — طبق محلي لليوم {dayNumber}"
            : $"Lunch — local specialty for day {dayNumber}";
    }
}
