namespace Safarni.Application.Features.Trips.GenerateItinerary;

/// <summary>
/// Applies verified flight windows deterministically before final validation.
/// AI output outside those windows is removed instead of forcing another large
/// completion request. A constrained day that becomes empty receives one safe
/// administrative item so the saved itinerary remains structurally usable.
/// </summary>
internal static class ItineraryFlightWindowResolver
{
    public static int Normalize(
        IReadOnlyList<AiItineraryDay> days,
        string city,
        string languageCode,
        ItineraryFlightPlan? flightPlan,
        int totalTripDays)
    {
        if (flightPlan is null)
        {
            return 0;
        }

        var adjustments = 0;

        foreach (var day in days)
        {
            var minimumStart = day.DayNumber == 1
                ? flightPlan.FirstDayAvailableFrom
                : null;
            var maximumEnd = day.DayNumber == totalTripDays
                ? flightPlan.LastDayMustEndBy
                : null;

            if (minimumStart is null && maximumEnd is null)
            {
                continue;
            }

            var invalidItems = day.Items
                .Where(item => !FitsWindow(
                    item,
                    minimumStart,
                    maximumEnd))
                .ToList();

            foreach (var invalidItem in invalidItems)
            {
                day.Items.Remove(invalidItem);
                adjustments++;
            }

            if (day.Items.Count == 0)
            {
                var fallback = CreateAdministrativeItem(
                    city,
                    languageCode,
                    minimumStart,
                    maximumEnd);

                if (fallback is not null)
                {
                    day.Items.Add(fallback);
                    adjustments++;
                }
            }

            SortAndReindex(day);
        }

        return adjustments;
    }

    private static bool FitsWindow(
        AiItineraryItem item,
        TimeSpan? minimumStart,
        TimeSpan? maximumEnd)
    {
        var start = ItineraryItemInput.ParseTime(item.StartTime);
        var end = ItineraryItemInput.ParseTime(item.EndTime);

        if (start is null || end is null)
        {
            return false;
        }

        return (minimumStart is null || start >= minimumStart) &&
               (maximumEnd is null || end <= maximumEnd);
    }

    private static AiItineraryItem? CreateAdministrativeItem(
        string city,
        string languageCode,
        TimeSpan? minimumStart,
        TimeSpan? maximumEnd)
    {
        var isArabic =
            ItineraryTextPolicy.IsArabicLanguage(languageCode);

        if (maximumEnd is { } cutoff)
        {
            var end = cutoff > TimeSpan.FromMinutes(5)
                ? cutoff - TimeSpan.FromMinutes(5)
                : cutoff;
            var earliestStart = minimumStart ?? TimeSpan.Zero;
            var start = end - TimeSpan.FromMinutes(20);

            if (start < earliestStart)
            {
                start = earliestStart;
            }

            if (end <= start)
            {
                return null;
            }

            return new AiItineraryItem
            {
                StartTime = FormatTime(start),
                EndTime = FormatTime(end),
                Title = isArabic
                    ? "الاستعداد للمطار"
                    : "Airport preparation",
                Description = isArabic
                    ? "الاستعداد للمغادرة ضمن موعد الرحلة المؤكد."
                    : "Prepare for departure within the verified flight window.",
                Location = city,
                PlaceReference = null,
                Category = "other",
                EstimatedCost = 0m,
                OrderIndex = 0
            };
        }

        if (minimumStart is { } availableFrom)
        {
            var latestEnd = new TimeSpan(23, 59, 0);
            var end = availableFrom.Add(TimeSpan.FromMinutes(30));

            if (end > latestEnd)
            {
                end = latestEnd;
            }

            if (end <= availableFrom)
            {
                return null;
            }

            return new AiItineraryItem
            {
                StartTime = FormatTime(availableFrom),
                EndTime = FormatTime(end),
                Title = isArabic
                    ? "الوصول والاستقرار"
                    : "Arrival and settle in",
                Description = isArabic
                    ? "وقت مرن بعد الوصول المؤكد."
                    : "Flexible time after the verified arrival.",
                Location = city,
                PlaceReference = null,
                Category = "other",
                EstimatedCost = 0m,
                OrderIndex = 0
            };
        }

        return null;
    }

    private static void SortAndReindex(AiItineraryDay day)
    {
        day.Items.Sort((left, right) =>
        {
            var leftStart =
                ItineraryItemInput.ParseTime(left.StartTime) ??
                TimeSpan.MaxValue;
            var rightStart =
                ItineraryItemInput.ParseTime(right.StartTime) ??
                TimeSpan.MaxValue;

            var startComparison = leftStart.CompareTo(rightStart);
            return startComparison != 0
                ? startComparison
                : left.OrderIndex.CompareTo(right.OrderIndex);
        });

        for (var index = 0;
             index < day.Items.Count;
             index++)
        {
            day.Items[index].OrderIndex = index;
        }
    }

    private static string FormatTime(TimeSpan value) =>
        value.ToString(@"hh\:mm");
}
