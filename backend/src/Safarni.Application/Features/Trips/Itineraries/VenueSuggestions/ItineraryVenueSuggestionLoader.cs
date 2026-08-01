using ChatBotApi.Application.Common.Models.GooglePlaces;
using Safarni.Application.Abstractions.Travel;
using Safarni.Application.Features.Trips.GenerateItinerary;
using Safarni.Domain.Entities;

namespace Safarni.Application.Features.Trips.Itineraries.VenueSuggestions;

public sealed record LoadedVenueSuggestion(
    PlaceDetails Place,
    int? StayNights);

/// <summary>
/// Server-side source of truth for selectable venues. The selection command
/// sends only a place ID and reloads the place from the live provider instead
/// of trusting prices, coordinates, ratings, or URLs posted by the browser.
/// </summary>
public sealed class ItineraryVenueSuggestionLoader
{
    private readonly IItineraryTravelDataService _travelData;

    public ItineraryVenueSuggestionLoader(
        IItineraryTravelDataService travelData)
    {
        _travelData = travelData;
    }

    public async Task<IReadOnlyList<LoadedVenueSuggestion>> LoadAsync(
        Trip trip,
        Itinerary itinerary,
        ItineraryDay day,
        ItineraryItem item,
        CancellationToken cancellationToken)
    {
        var schedule = TripRouteSerializer.ReadFromTrip(trip);
        var cityInfo = GetCityForDay(schedule, day.DayNumber);
        if (cityInfo is null)
        {
            return [];
        }

        var preferences = GenerateItineraryPreferencesSerializer
            .Deserialize(itinerary.Preferences);

        IReadOnlyList<PlaceDetails> places;
        int? stayNights = null;

        if (item.Category == ItineraryItemCategory.Accommodation)
        {
            stayNights = GetNightCount(
                schedule,
                cityInfo.Value.Index,
                cityInfo.Value.City.Days);

            if (stayNights <= 0)
            {
                return [];
            }

            var checkIn = trip.StartDate.Date
                .AddDays(cityInfo.Value.StartDay - 1);
            var checkOut = checkIn.AddDays(stayNights.Value);

            places = await _travelData.SearchHotelsAsync(
                cityInfo.Value.City.Name,
                preferences.AccommodationStyle,
                checkIn,
                checkOut,
                itinerary.TravelerCount,
                itinerary.CurrencyCode,
                preferences.PreferredLanguage,
                cancellationToken);
        }
        else if (item.Category == ItineraryItemCategory.Meal)
        {
            var mealPeriod = GetMealPeriod(item);
            places = await _travelData.SearchLocalAsync(
                $"popular {mealPeriod} restaurants and cafes in {cityInfo.Value.City.Name}",
                cityInfo.Value.City.Name,
                preferences.PreferredLanguage,
                cancellationToken);
        }
        else
        {
            return [];
        }

        var eligible = places
            .Where(place =>
                !string.IsNullOrWhiteSpace(place.PlaceId) &&
                !string.IsNullOrWhiteSpace(place.Name))
            .Where(place =>
                !string.Equals(
                    place.PlaceId,
                    item.GooglePlaceId,
                    StringComparison.OrdinalIgnoreCase))
            .ToList();

        var cityMatched = eligible
            .Where(place =>
                !string.IsNullOrWhiteSpace(place.Address) &&
                place.Address.Contains(
                    cityInfo.Value.City.Name,
                    StringComparison.OrdinalIgnoreCase))
            .ToList();

        return (cityMatched.Count > 0 ? cityMatched : eligible)
            .Take(12)
            .Select(place => new LoadedVenueSuggestion(place, stayNights))
            .ToList();
    }

    public async Task<LoadedVenueSuggestion?> FindByPlaceIdAsync(
        Trip trip,
        Itinerary itinerary,
        ItineraryDay day,
        ItineraryItem item,
        string placeId,
        CancellationToken cancellationToken)
    {
        var suggestions = await LoadAsync(
            trip,
            itinerary,
            day,
            item,
            cancellationToken);

        return suggestions.FirstOrDefault(candidate =>
            string.Equals(
                candidate.Place.PlaceId,
                placeId,
                StringComparison.OrdinalIgnoreCase));
    }

    private static (TripCitySelection City, int StartDay, int Index)?
        GetCityForDay(
            IReadOnlyList<TripCitySelection> cities,
            int dayNumber)
    {
        var cursor = 1;

        for (var index = 0; index < cities.Count; index++)
        {
            var city = cities[index];
            if (dayNumber >= cursor &&
                dayNumber < cursor + city.Days)
            {
                return (city, cursor, index);
            }

            cursor += city.Days;
        }

        return null;
    }

    private static int GetNightCount(
        IReadOnlyList<TripCitySelection> cities,
        int cityIndex,
        int cityDays)
    {
        var isLastCity = cityIndex == cities.Count - 1;
        return isLastCity
            ? Math.Max(0, cityDays - 1)
            : Math.Max(0, cityDays);
    }

    private static string GetMealPeriod(ItineraryItem item)
    {
        if (item.Title.Contains(
                "breakfast",
                StringComparison.OrdinalIgnoreCase) ||
            item.StartTime < TimeSpan.FromHours(11))
        {
            return "breakfast";
        }

        if (item.Title.Contains(
                "dinner",
                StringComparison.OrdinalIgnoreCase) ||
            item.StartTime >= TimeSpan.FromHours(17))
        {
            return "dinner";
        }

        return "lunch";
    }
}
