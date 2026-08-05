using Safarni.Domain.Entities;

namespace Safarni.Application.Features.Trips.GenerateItinerary;

internal enum ItineraryMinimumActivityRepairStatus
{
    AlreadySatisfied,
    Inserted,
    NoVerifiedCandidate,
    NoEligibleDay,
    NoFeasibleWindow
}

internal sealed record ItineraryMinimumActivityRepairResult(
    ItineraryMinimumActivityRepairStatus Status,
    string City,
    int? DayNumber = null,
    string? PlaceReference = null,
    string? PlaceName = null,
    TimeSpan? Start = null,
    TimeSpan? End = null,
    IReadOnlyList<string>? RemovedItems = null);

/// <summary>
/// Guarantees one verified activity per city whenever the real schedule can
/// support it. Transport and accommodation are protected. Optional AI-created
/// rest/free-time blocks are reclaimed before meals, so a crowded model output
/// cannot fail the complete generation job merely because it omitted a place.
/// </summary>
internal static class ItineraryMinimumActivityResolver
{
    private static readonly TimeSpan DayStart = TimeSpan.FromHours(8);
    private static readonly TimeSpan DayEnd = TimeSpan.FromHours(22);
    private static readonly TimeSpan PreferredDuration =
        TimeSpan.FromMinutes(90);
    private static readonly TimeSpan MinimumDuration =
        TimeSpan.FromMinutes(60);
    private static readonly TimeSpan SearchStep =
        TimeSpan.FromMinutes(15);

    public static ItineraryMinimumActivityRepairResult Ensure(
        IReadOnlyList<AiItineraryDay> days,
        IReadOnlyList<ItineraryPlaceCandidate> places,
        ItineraryCityStop cityStop,
        string languageCode,
        ItineraryFlightPlan? flightPlan,
        int totalTripDays)
    {
        var activityCandidates = places
            .Where(candidate =>
                candidate.Purpose == ItineraryPlacePurpose.Activity &&
                string.Equals(
                    candidate.City,
                    cityStop.City,
                    StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(candidate =>
                !string.IsNullOrWhiteSpace(candidate.Place.PhotoUrl))
            .ThenByDescending(candidate => candidate.Place.Rating ?? 0)
            .ThenByDescending(candidate =>
                candidate.Place.UserRatingsTotal ?? 0)
            .ToList();

        if (activityCandidates.Count == 0)
        {
            return new ItineraryMinimumActivityRepairResult(
                ItineraryMinimumActivityRepairStatus.NoVerifiedCandidate,
                cityStop.City);
        }

        var eligibleDays = days
            .Where(day =>
                ItineraryOutputParser.HasUsableActivityWindow(
                    day.DayNumber,
                    flightPlan,
                    totalTripDays))
            .OrderBy(day => day.DayNumber)
            .ToList();

        if (eligibleDays.SelectMany(day => day.Items).Any(IsActivity))
        {
            return new ItineraryMinimumActivityRepairResult(
                ItineraryMinimumActivityRepairStatus.AlreadySatisfied,
                cityStop.City);
        }

        if (eligibleDays.Count == 0)
        {
            return new ItineraryMinimumActivityRepairResult(
                ItineraryMinimumActivityRepairStatus.NoEligibleDay,
                cityStop.City);
        }

        var usedReferences = days
            .SelectMany(day => day.Items)
            .Where(item =>
                ItineraryItemInput.ParseCategory(item.Category) is
                    ItineraryItemCategory.Activity or
                    ItineraryItemCategory.Meal)
            .Select(item => item.PlaceReference)
            .Where(reference => !string.IsNullOrWhiteSpace(reference))
            .Select(reference => reference!.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var selectedCandidate = activityCandidates.FirstOrDefault(candidate =>
            !usedReferences.Contains(candidate.Reference)) ??
            activityCandidates[0];
        var bestPlan = eligibleDays
            .Select(day => FindBestPlan(
                day,
                flightPlan,
                totalTripDays))
            .Where(plan => plan is not null)
            .Select(plan => plan!)
            .OrderBy(plan => plan.Score)
            .ThenBy(plan => plan.Day.DayNumber)
            .ThenBy(plan => plan.Start)
            .FirstOrDefault();

        if (bestPlan is null)
        {
            return new ItineraryMinimumActivityRepairResult(
                ItineraryMinimumActivityRepairStatus.NoFeasibleWindow,
                cityStop.City);
        }

        var removedTitles = bestPlan.ItemsToRemove
            .Select(item => item.Title)
            .ToList();

        foreach (var item in bestPlan.ItemsToRemove)
        {
            bestPlan.Day.Items.Remove(item);
        }

        bestPlan.Day.Items.Add(
            new AiItineraryItem
            {
                StartTime = FormatTime(bestPlan.Start),
                EndTime = FormatTime(bestPlan.End),
                Title = ItineraryTextPolicy.BuildActivityTitle(
                    selectedCandidate.Place,
                    languageCode),
                Description =
                    ItineraryTextPolicy.BuildActivityDescription(
                        languageCode),
                Location = selectedCandidate.Place.Name,
                PlaceReference = selectedCandidate.Reference,
                Category = "activity",
                EstimatedCost = null,
                OrderIndex = bestPlan.Day.Items.Count
            });
        SortAndReindex(bestPlan.Day);

        return new ItineraryMinimumActivityRepairResult(
            ItineraryMinimumActivityRepairStatus.Inserted,
            cityStop.City,
            bestPlan.Day.DayNumber,
            selectedCandidate.Reference,
            selectedCandidate.Place.Name,
            bestPlan.Start,
            bestPlan.End,
            removedTitles);
    }

    public static bool HasFeasibleWindow(
        AiItineraryDay day,
        ItineraryFlightPlan? flightPlan,
        int totalTripDays)
    {
        return ItineraryOutputParser.HasUsableActivityWindow(
                   day.DayNumber,
                   flightPlan,
                   totalTripDays) &&
               FindBestPlan(
                   day,
                   flightPlan,
                   totalTripDays) is not null;
    }

    private static ActivityInsertionPlan? FindBestPlan(
        AiItineraryDay day,
        ItineraryFlightPlan? flightPlan,
        int totalTripDays)
    {
        var (windowStart, windowEnd) = ResolveWindow(
            day.DayNumber,
            flightPlan,
            totalTripDays);

        if (windowEnd - windowStart < PreferredDuration)
        {
            return null;
        }

        var scheduledItems = day.Items
            .Select(item => new ScheduledItem(
                item,
                ItineraryItemInput.ParseTime(item.StartTime),
                ItineraryItemInput.ParseTime(item.EndTime)))
            .Where(item =>
                item.Start is not null &&
                item.End is not null &&
                item.End > item.Start)
            .ToList();
        var candidateStarts = new HashSet<TimeSpan> { windowStart };

        for (var start = windowStart;
             start <= windowEnd - MinimumDuration;
             start += SearchStep)
        {
            candidateStarts.Add(start);
        }

        foreach (var scheduled in scheduledItems)
        {
            if (scheduled.End is { } end &&
                end >= windowStart &&
                end <= windowEnd - MinimumDuration)
            {
                candidateStarts.Add(end);
            }
        }

        ActivityInsertionPlan? bestPlan = null;

        foreach (var duration in new[]
                 {
                     PreferredDuration,
                     MinimumDuration
                 })
        {
            foreach (var start in candidateStarts.OrderBy(value => value))
            {
                var end = start + duration;
                if (start < windowStart || end > windowEnd)
                {
                    continue;
                }

                var overlappingItems = scheduledItems
                    .Where(item => Overlaps(
                        item.Start!.Value,
                        item.End!.Value,
                        start,
                        end))
                    .ToList();

                if (overlappingItems.Any(item =>
                        IsProtected(item.Item)))
                {
                    continue;
                }

                var optionalItems = overlappingItems
                    .Select(item => item.Item)
                    .Distinct()
                    .ToList();
                var score = CalculateScore(
                    optionalItems,
                    duration);
                var plan = new ActivityInsertionPlan(
                    day,
                    start,
                    end,
                    optionalItems,
                    score);

                if (bestPlan is null ||
                    plan.Score < bestPlan.Score ||
                    plan.Score == bestPlan.Score &&
                    plan.Start < bestPlan.Start)
                {
                    bestPlan = plan;
                }
            }
        }

        return bestPlan;
    }

    private static (TimeSpan Start, TimeSpan End) ResolveWindow(
        int dayNumber,
        ItineraryFlightPlan? flightPlan,
        int totalTripDays)
    {
        var start = DayStart;
        var end = DayEnd;

        if (dayNumber == 1 &&
            flightPlan?.FirstDayAvailableFrom is { } availableFrom &&
            availableFrom > start)
        {
            start = availableFrom;
        }

        if (dayNumber == totalTripDays &&
            flightPlan?.LastDayMustEndBy is { } mustEndBy &&
            mustEndBy < end)
        {
            end = mustEndBy;
        }

        return (start, end);
    }

    private static long CalculateScore(
        IReadOnlyList<AiItineraryItem> itemsToRemove,
        TimeSpan duration)
    {
        var mealCount = itemsToRemove.Count(item =>
            ItineraryItemInput.ParseCategory(item.Category) ==
            ItineraryItemCategory.Meal);
        var otherCount = itemsToRemove.Count - mealCount;
        var shortDurationPenalty = duration < PreferredDuration
            ? 50L
            : 0L;

        return mealCount * 100_000L +
               otherCount * 1_000L +
               itemsToRemove.Count * 10L +
               shortDurationPenalty;
    }

    private static bool IsProtected(AiItineraryItem item)
    {
        return ItineraryItemInput.ParseCategory(item.Category) is
            ItineraryItemCategory.Transport or
            ItineraryItemCategory.Accommodation or
            ItineraryItemCategory.Activity;
    }

    private static bool IsActivity(AiItineraryItem item)
    {
        return ItineraryItemInput.ParseCategory(item.Category) ==
               ItineraryItemCategory.Activity;
    }

    private static bool Overlaps(
        TimeSpan itemStart,
        TimeSpan itemEnd,
        TimeSpan windowStart,
        TimeSpan windowEnd)
    {
        return itemStart < windowEnd && itemEnd > windowStart;
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

        for (var index = 0; index < day.Items.Count; index++)
        {
            day.Items[index].OrderIndex = index;
        }
    }

    private static string FormatTime(TimeSpan value) =>
        value.ToString(@"hh\:mm");

    private sealed record ScheduledItem(
        AiItineraryItem Item,
        TimeSpan? Start,
        TimeSpan? End);

    private sealed record ActivityInsertionPlan(
        AiItineraryDay Day,
        TimeSpan Start,
        TimeSpan End,
        IReadOnlyList<AiItineraryItem> ItemsToRemove,
        long Score);
}
