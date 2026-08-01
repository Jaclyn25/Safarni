using Safarni.Domain.Entities;

namespace Safarni.Application.Features.Trips.GenerateItinerary;

internal static class ItineraryAccommodationResolver
{
    public static int NormalizeCityStay(
        IReadOnlyList<AiItineraryDay> days,
        ItineraryCityStop cityStop,
        GenerateItineraryPreferences preferences,
        IReadOnlyList<ItineraryPlaceCandidate> places,
        ItineraryFlightPlan? flightPlan = null,
        int? totalTripDays = null)
    {
        var firstDay = days.FirstOrDefault(day =>
            day.DayNumber == cityStop.StartDayNumber);

        if (firstDay is null)
        {
            return 0;
        }

        var accommodationEntries = days
            .SelectMany(day =>
                day.Items
                    .Where(IsAccommodation)
                    .Select(item =>
                        (Day: day, Item: item)))
            .ToList();

        var alreadyBooked = string.Equals(
            preferences.AccommodationStyle,
            "already_booked",
            StringComparison.OrdinalIgnoreCase);

        var verifiedAccommodation = places
            .Where(place =>
                place.Purpose ==
                    ItineraryPlacePurpose.Accommodation &&
                string.Equals(
                    place.City,
                    cityStop.City,
                    StringComparison.OrdinalIgnoreCase))
            .ToList();

        // The last selected city can have zero hotel nights. In that case an
        // accommodation recommendation would be misleading even if an AI item
        // was returned accidentally.
        if (alreadyBooked ||
            cityStop.NightCount <= 0 ||
            verifiedAccommodation.Count == 0)
        {
            foreach (var entry in accommodationEntries)
            {
                entry.Day.Items.Remove(entry.Item);
            }

            Reindex(days);
            return accommodationEntries.Count;
        }

        var safeWindow = ResolveSafeCheckInWindow(
            cityStop,
            firstDay,
            flightPlan,
            totalTripDays);

        if (safeWindow is null)
        {
            foreach (var entry in accommodationEntries)
            {
                entry.Day.Items.Remove(entry.Item);
            }

            Reindex(days);
            return accommodationEntries.Count;
        }

        var selectedItem = accommodationEntries
            .Where(entry =>
                ItineraryPlaceCatalog.FindPlace(
                    verifiedAccommodation,
                    entry.Item.PlaceReference) is not null)
            .OrderBy(entry =>
                entry.Day.DayNumber ==
                cityStop.StartDayNumber
                    ? 0
                    : 1)
            .ThenBy(entry => entry.Day.DayNumber)
            .ThenBy(entry => entry.Item.OrderIndex)
            .Select(entry => entry.Item)
            .FirstOrDefault();

        var adjustments =
            accommodationEntries.Count > 1
                ? accommodationEntries.Count - 1
                : 0;

        if (selectedItem is null)
        {
            selectedItem = CreateAccommodationItem(
                verifiedAccommodation[0],
                preferences.PreferredLanguage,
                safeWindow.Value.Start,
                safeWindow.Value.End);
            adjustments++;
        }
        else
        {
            var selectedCandidate =
                ItineraryPlaceCatalog.FindCandidate(
                    verifiedAccommodation,
                    selectedItem.PlaceReference)
                ?? verifiedAccommodation[0];

            selectedItem.Category = "accommodation";
            selectedItem.PlaceReference = selectedCandidate.Reference;
            selectedItem.Location = selectedCandidate.Place.Name;
            selectedItem.Title =
                ItineraryTextPolicy.BuildAccommodationTitle(
                    selectedCandidate.Place,
                    preferences.PreferredLanguage);
            selectedItem.Description =
                ItineraryTextPolicy.BuildAccommodationDescription(
                    preferences.PreferredLanguage);
            selectedItem.EstimatedCost = null;
            selectedItem.StartTime =
                safeWindow.Value.Start.ToString(@"hh\:mm");
            selectedItem.EndTime =
                safeWindow.Value.End.ToString(@"hh\:mm");
        }

        foreach (var entry in accommodationEntries)
        {
            entry.Day.Items.Remove(entry.Item);
        }

        firstDay.Items.Insert(0, selectedItem);
        Reindex(days);

        if (accommodationEntries.Count == 1 &&
            accommodationEntries[0].Day != firstDay)
        {
            adjustments++;
        }

        return adjustments;
    }

    private static (TimeSpan Start, TimeSpan End)?
        ResolveSafeCheckInWindow(
            ItineraryCityStop cityStop,
            AiItineraryDay firstDay,
            ItineraryFlightPlan? flightPlan,
            int? totalTripDays)
    {
        var earliestStart = TimeSpan.FromHours(15);

        if (cityStop.StartDayNumber == 1 &&
            flightPlan?.FirstDayAvailableFrom is { } availableFrom &&
            availableFrom > earliestStart)
        {
            earliestStart = availableFrom;
        }

        var latestEnd = new TimeSpan(23, 59, 0);

        if (totalTripDays is { } tripDays &&
            cityStop.StartDayNumber == tripDays &&
            flightPlan?.LastDayMustEndBy is { } mustEndBy &&
            mustEndBy < latestEnd)
        {
            latestEnd = mustEndBy;
        }

        const int checkInMinutes = 15;
        var checkInDuration = TimeSpan.FromMinutes(checkInMinutes);
        var occupiedWindows = firstDay.Items
            .Where(item => !IsAccommodation(item))
            .Select(item => new
            {
                Start = ItineraryItemInput.ParseTime(item.StartTime),
                End = ItineraryItemInput.ParseTime(item.EndTime)
            })
            .Where(window =>
                window.Start is not null &&
                window.End is not null)
            .Select(window =>
                (Start: window.Start!.Value, End: window.End!.Value))
            .OrderBy(window => window.Start)
            .ToList();

        var candidateStart = earliestStart;

        foreach (var window in occupiedWindows)
        {
            if (window.End <= candidateStart)
            {
                continue;
            }

            var candidateEnd = candidateStart + checkInDuration;

            if (candidateEnd <= window.Start &&
                candidateEnd <= latestEnd)
            {
                return (candidateStart, candidateEnd);
            }

            if (window.Start < candidateEnd)
            {
                candidateStart = window.End;
            }
        }

        var finalEnd = candidateStart + checkInDuration;

        return finalEnd <= latestEnd
            ? (candidateStart, finalEnd)
            : null;
    }

    private static AiItineraryItem CreateAccommodationItem(
        ItineraryPlaceCandidate place,
        string languageCode,
        TimeSpan start,
        TimeSpan end)
    {
        return new AiItineraryItem
        {
            StartTime = start.ToString(@"hh\:mm"),
            EndTime = end.ToString(@"hh\:mm"),
            Title = ItineraryTextPolicy.BuildAccommodationTitle(
                place.Place,
                languageCode),
            Description =
                ItineraryTextPolicy.BuildAccommodationDescription(
                    languageCode),
            Location = place.Place.Name,
            PlaceReference = place.Reference,
            Category = "accommodation",
            EstimatedCost = null,
            OrderIndex = 0
        };
    }

    private static bool IsAccommodation(
        AiItineraryItem item)
    {
        return ItineraryItemInput.ParseCategory(
                   item.Category) ==
               ItineraryItemCategory.Accommodation;
    }

    private static void Reindex(
        IReadOnlyList<AiItineraryDay> days)
    {
        foreach (var day in days)
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
    }
}
