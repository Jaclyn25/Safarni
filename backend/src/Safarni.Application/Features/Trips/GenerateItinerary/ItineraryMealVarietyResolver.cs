using Safarni.Domain.Entities;

namespace Safarni.Application.Features.Trips.GenerateItinerary;

internal static class ItineraryMealVarietyResolver
{
    public static int ResolveCrossSegmentDuplicates(
        IReadOnlyList<AiItineraryDay> days,
        string city,
        string languageCode,
        ISet<string> usedSuggestions)
    {
        var adjustedCount = 0;
        var isArabic = string.Equals(
            languageCode,
            "ar",
            StringComparison.OrdinalIgnoreCase);

        foreach (var day in days
                     .OrderBy(value => value.DayNumber))
        {
            foreach (var meal in day.Items
                         .Where(item =>
                             ItineraryItemInput
                                 .ParseCategory(
                                     item.Category) ==
                             ItineraryItemCategory.Meal)
                         .OrderBy(item =>
                             item.OrderIndex))
            {
                var suggestion =
                    ItineraryOutputParser
                        .GetMealSuggestionKey(
                            meal.Title);

                if (usedSuggestions.Add(suggestion))
                {
                    continue;
                }

                meal.Title = BuildFallbackTitle(
                    meal,
                    city,
                    day.DayNumber,
                    isArabic);
                meal.Description = isArabic
                    ? "اختر من خيارات المطاعم القريبة."
                    : "Choose from nearby restaurant options.";

                var fallbackSuggestion =
                    ItineraryOutputParser
                        .GetMealSuggestionKey(
                            meal.Title);

                usedSuggestions.Add(
                    fallbackSuggestion);
                adjustedCount++;
            }
        }

        return adjustedCount;
    }

    private static string BuildFallbackTitle(
        AiItineraryItem meal,
        string city,
        int dayNumber,
        bool isArabic)
    {
        var slot = GetMealSlot(
            meal);

        if (isArabic)
        {
            var arabicSlot = slot switch
            {
                MealSlot.Breakfast => "إفطار",
                MealSlot.Dinner => "عشاء",
                _ => "غداء"
            };

            return $"{arabicSlot} — طبق {arabicSlot} محلي مختلف في {city} لليوم {dayNumber}";
        }

        var englishSlot = slot switch
        {
            MealSlot.Breakfast => "Breakfast",
            MealSlot.Dinner => "Dinner",
            _ => "Lunch"
        };

        return $"{englishSlot} — a different local {englishSlot.ToLowerInvariant()} specialty in {city} for day {dayNumber}";
    }

    private static MealSlot GetMealSlot(
        AiItineraryItem meal)
    {
        if (meal.Title.Contains(
                "breakfast",
                StringComparison.OrdinalIgnoreCase) ||
            ParseTime(meal.StartTime) <
            TimeSpan.FromHours(11))
        {
            return MealSlot.Breakfast;
        }

        if (meal.Title.Contains(
                "dinner",
                StringComparison.OrdinalIgnoreCase) ||
            ParseTime(meal.StartTime) >=
            TimeSpan.FromHours(17))
        {
            return MealSlot.Dinner;
        }

        return MealSlot.Lunch;
    }

    private static TimeSpan ParseTime(
        string? value)
    {
        return TimeSpan.TryParse(
            value,
            out var time)
                ? time
                : TimeSpan.Zero;
    }

    private enum MealSlot
    {
        Breakfast,
        Lunch,
        Dinner
    }
}
