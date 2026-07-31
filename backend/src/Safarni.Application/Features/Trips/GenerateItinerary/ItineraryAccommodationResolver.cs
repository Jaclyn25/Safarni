using Safarni.Domain.Entities;

namespace Safarni.Application.Features.Trips.GenerateItinerary;

internal static class ItineraryAccommodationResolver
{
    public static int NormalizeCityStay(
        IReadOnlyList<AiItineraryDay> days,
        ItineraryCityStop cityStop,
        GenerateItineraryPreferences preferences,
        IReadOnlyList<ItineraryPlaceCandidate> places)
    {
        var firstDay = days.FirstOrDefault(day =>
            day.DayNumber ==
            cityStop.StartDayNumber);

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
                ItineraryPlacePurpose.Accommodation)
            .ToList();

        if (alreadyBooked ||
            verifiedAccommodation.Count == 0)
        {
            foreach (var entry in
                     accommodationEntries)
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
            .ThenBy(entry =>
                entry.Day.DayNumber)
            .ThenBy(entry =>
                entry.Item.OrderIndex)
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
                preferences.PreferredLanguage);
            adjustments++;
        }

        foreach (var entry in accommodationEntries)
        {
            entry.Day.Items.Remove(entry.Item);
        }

        firstDay.Items.Insert(
            0,
            selectedItem);
        Reindex(days);

        if (accommodationEntries.Count == 1 &&
            accommodationEntries[0].Day != firstDay)
        {
            adjustments++;
        }

        return adjustments;
    }

    private static AiItineraryItem
        CreateAccommodationItem(
            ItineraryPlaceCandidate place,
            string languageCode)
    {
        var isArabic = string.Equals(
            languageCode,
            "ar",
            StringComparison.OrdinalIgnoreCase);

        return new AiItineraryItem
        {
            StartTime = "15:00",
            EndTime = "15:15",
            Title = place.Place.Name,
            Description = isArabic
                ? "إقامة مقترحة لهذه المدينة."
                : "Recommended stay for this city.",
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
            for (var index = 0;
                 index < day.Items.Count;
                 index++)
            {
                day.Items[index].OrderIndex = index;
            }
        }
    }
}
