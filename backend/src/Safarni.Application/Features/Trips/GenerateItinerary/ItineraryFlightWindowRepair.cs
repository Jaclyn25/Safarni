using Safarni.Domain.Entities;

namespace Safarni.Application.Features.Trips.GenerateItinerary;

internal static class ItineraryFlightWindowRepair
{
    private static readonly TimeSpan EndOfDay = new(23, 59, 0);

    public static int Repair(
        IReadOnlyList<AiItineraryDay> days,
        ItineraryFlightPlan flightPlan,
        int totalTripDays)
    {
        return Repair(
            days,
            flightPlan.FirstDayAvailableFrom,
            flightPlan.LastDayMustEndBy,
            totalTripDays);
    }

    internal static int Repair(
        IReadOnlyList<AiItineraryDay> days,
        TimeSpan? firstDayAvailableFrom,
        TimeSpan? lastDayMustEndBy,
        int totalTripDays)
    {
        var adjustments = 0;

        if (firstDayAvailableFrom is { } availableFrom)
        {
            var firstDay = days.FirstOrDefault(day => day.DayNumber == 1);
            if (firstDay is not null)
            {
                adjustments += RepairArrivalDay(firstDay, availableFrom);
            }
        }

        if (lastDayMustEndBy is { } mustEndBy)
        {
            var lastDay = days.FirstOrDefault(day =>
                day.DayNumber == totalTripDays);

            if (lastDay is not null)
            {
                adjustments += RepairDepartureDay(lastDay, mustEndBy);
            }
        }

        foreach (var day in days)
        {
            for (var index = 0; index < day.Items.Count; index++)
            {
                day.Items[index].OrderIndex = index;
            }
        }

        return adjustments;
    }

    private static int RepairArrivalDay(
        AiItineraryDay day,
        TimeSpan availableFrom)
    {
        // HH:mm itinerary items cannot represent a time on the next calendar
        // day. If the verified arrival buffer crosses midnight, leave the
        // segment for deterministic validation/retry instead of inventing time.
        if (availableFrom >= TimeSpan.FromDays(1))
        {
            return 0;
        }

        var adjustments = 0;

        for (var index = day.Items.Count - 1; index >= 0; index--)
        {
            var item = day.Items[index];
            var start = ItineraryItemInput.ParseTime(item.StartTime);

            if (start is null || start >= availableFrom)
            {
                continue;
            }

            if (CanMoveAdministrativeItem(item) &&
                TryMoveAfter(item, availableFrom))
            {
                adjustments++;
                continue;
            }

            // Meals, sightseeing, and other optional content before the
            // traveler can physically reach the city are impossible. Remove
            // them instead of spending another AI request on a correctable
            // scheduling mistake.
            day.Items.RemoveAt(index);
            adjustments++;
        }

        return adjustments;
    }

    private static int RepairDepartureDay(
        AiItineraryDay day,
        TimeSpan mustEndBy)
    {
        if (mustEndBy <= TimeSpan.Zero)
        {
            return 0;
        }

        var adjustments = 0;

        for (var index = day.Items.Count - 1; index >= 0; index--)
        {
            var item = day.Items[index];
            var end = ItineraryItemInput.ParseTime(item.EndTime);

            if (end is null || end <= mustEndBy)
            {
                continue;
            }

            if (CanMoveAdministrativeItem(item) &&
                TryMoveBefore(item, mustEndBy))
            {
                adjustments++;
                continue;
            }

            day.Items.RemoveAt(index);
            adjustments++;
        }

        return adjustments;
    }

    private static bool CanMoveAdministrativeItem(
        AiItineraryItem item)
    {
        if (ItineraryItemInput.ParseCategory(item.Category) ==
            ItineraryItemCategory.Accommodation)
        {
            return true;
        }

        if (ItineraryItemInput.ParseCategory(item.Category) !=
            ItineraryItemCategory.Other)
        {
            return false;
        }

        var text = $"{item.Title} {item.Description}";

        return ContainsAny(
            text,
            "check in",
            "check-in",
            "checkin",
            "check out",
            "check-out",
            "checkout",
            "arrival",
            "departure",
            "settle in",
            "airport preparation",
            "prepare for airport",
            "prepare for departure",
            "rest",
            "تسجيل الوصول",
            "تسجيل المغادرة",
            "الوصول",
            "المغادرة",
            "الاستعداد للمطار",
            "راحة");
    }

    private static bool TryMoveAfter(
        AiItineraryItem item,
        TimeSpan availableFrom)
    {
        if (availableFrom >= EndOfDay)
        {
            return false;
        }

        var duration = ResolveDuration(item);
        var end = availableFrom + duration;

        if (end > EndOfDay)
        {
            end = EndOfDay;
        }

        if (end <= availableFrom)
        {
            return false;
        }

        item.StartTime = FormatTime(availableFrom);
        item.EndTime = FormatTime(end);
        return true;
    }

    private static bool TryMoveBefore(
        AiItineraryItem item,
        TimeSpan mustEndBy)
    {
        if (mustEndBy <= TimeSpan.Zero)
        {
            return false;
        }

        var existingStart = ItineraryItemInput.ParseTime(item.StartTime);

        if (existingStart is { } start && start < mustEndBy)
        {
            item.EndTime = FormatTime(mustEndBy);
            return true;
        }

        var duration = ResolveDuration(item);
        var movedStart = mustEndBy - duration;

        if (movedStart < TimeSpan.Zero)
        {
            movedStart = TimeSpan.Zero;
        }

        if (movedStart >= mustEndBy)
        {
            return false;
        }

        item.StartTime = FormatTime(movedStart);
        item.EndTime = FormatTime(mustEndBy);
        return true;
    }

    private static TimeSpan ResolveDuration(AiItineraryItem item)
    {
        var start = ItineraryItemInput.ParseTime(item.StartTime);
        var end = ItineraryItemInput.ParseTime(item.EndTime);

        if (start is { } parsedStart &&
            end is { } parsedEnd &&
            parsedEnd > parsedStart)
        {
            var original = parsedEnd - parsedStart;

            // Administrative/check-in blocks do not need to consume several
            // hours merely because the model originally scheduled them badly.
            return original > TimeSpan.FromMinutes(60)
                ? TimeSpan.FromMinutes(60)
                : original;
        }

        return TimeSpan.FromMinutes(30);
    }

    private static string FormatTime(TimeSpan value)
    {
        return value.ToString(@"hh\:mm");
    }

    private static bool ContainsAny(
        string value,
        params string[] phrases)
    {
        return phrases.Any(phrase =>
            value.Contains(
                phrase,
                StringComparison.OrdinalIgnoreCase));
    }
}