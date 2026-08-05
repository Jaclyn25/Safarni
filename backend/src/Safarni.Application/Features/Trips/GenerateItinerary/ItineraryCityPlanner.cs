using ChatBotApi.Application.Common.Interfaces;
using ChatBotApi.Application.Common.Models.GooglePlaces;
using Safarni.Domain.Entities;

namespace Safarni.Application.Features.Trips.GenerateItinerary;

internal sealed record ItineraryCityStop(
    string City,
    int StartDayNumber,
    int EndDayNumber,
    int NightCount,
    string? AirportCode,
    string? AirportName)
{
    public int DayCount => EndDayNumber - StartDayNumber + 1;

    public DateTime GetStartDate(Trip trip) =>
        trip.StartDate.Date.AddDays(StartDayNumber - 1);

    public DateTime GetEndDate(Trip trip) =>
        trip.StartDate.Date.AddDays(EndDayNumber - 1);

    public DateTime? GetCheckOutDate(Trip trip) =>
        NightCount <= 0
            ? null
            : GetStartDate(trip).AddDays(NightCount);
}

internal static class ItineraryCityPlanner
{
    public static IReadOnlyList<ItineraryCityStop> ResolveTravelerCities(
        Trip trip,
        IReadOnlyList<TripCitySelection> selectedCities)
    {
        var acceptedCities = selectedCities
            .Where(city => !string.IsNullOrWhiteSpace(city.Name))
            .Select(city => new ProposedCity(
                city.Name.Trim(),
                Math.Max(1, city.Days),
                TripRouteSerializer.NormalizeAirportCode(
                    city.AirportCode),
                string.IsNullOrWhiteSpace(city.AirportName)
                    ? null
                    : city.AirportName.Trim()))
            .ToList();

        if (acceptedCities.Count == 0)
        {
            throw new ItineraryOutputException(
                "At least one accepted city is required.");
        }

        return BuildTravelerSelectedStops(
            acceptedCities,
            GetTripDayCount(trip));
    }

    public static async Task<IReadOnlyList<ItineraryCityStop>> ResolveAsync(
        Trip trip,
        IGooglePlacesService googlePlacesService,
        IReadOnlyList<TripCitySelection>? selectedCities,
        CancellationToken cancellationToken)
    {
        if (selectedCities is { Count: > 0 })
        {
            return ResolveTravelerCities(trip, selectedCities);
        }

        var destinationResults =
            await googlePlacesService.TextSearchAsync(
                trip.Destination,
                cancellationToken,
                "en");

        if (destinationResults.Count > 0 &&
            !IsCountryDestination(destinationResults))
        {
            var totalDays = GetTripDayCount(trip);
            return
            [
                new ItineraryCityStop(
                    GetSingleCityName(
                        trip.Destination,
                        destinationResults),
                    1,
                    totalDays,
                    Math.Max(0, totalDays - 1),
                    null,
                    null)
            ];
        }

        throw new ItineraryOutputException(
            "Choose destination cities and allocate the trip days before generating the itinerary.");
    }

    public static ItineraryCityStop GetStopForDay(
        IReadOnlyList<ItineraryCityStop> cityPlan,
        int dayNumber)
    {
        return cityPlan.FirstOrDefault(stop =>
                   dayNumber >= stop.StartDayNumber &&
                   dayNumber <= stop.EndDayNumber)
               ?? throw new ItineraryOutputException(
                   $"Day {dayNumber} is not assigned to a validated city.");
    }

    public static string BuildPromptSchedule(
        Trip trip,
        IReadOnlyList<ItineraryCityStop> cityPlan)
    {
        return string.Join(
            Environment.NewLine,
            cityPlan.Select(stop =>
                $"- Days {stop.StartDayNumber}-{stop.EndDayNumber}: " +
                $"{stop.City} " +
                $"({stop.GetStartDate(trip):yyyy-MM-dd} to " +
                $"{stop.GetEndDate(trip):yyyy-MM-dd}; " +
                $"{stop.NightCount} hotel night(s))"));
    }

    private static IReadOnlyList<ItineraryCityStop>
        BuildTravelerSelectedStops(
            IReadOnlyList<ProposedCity> cities,
            int totalDays)
    {
        if (cities.Count > totalDays)
        {
            throw new ItineraryOutputException(
                "Choose more trip days before selecting more cities.");
        }

        if (cities.Sum(city => city.Days) != totalDays)
        {
            throw new ItineraryOutputException(
                "The city day allocations must equal the trip length.");
        }

        var stops = new List<ItineraryCityStop>();
        var nextDay = 1;

        for (var index = 0; index < cities.Count; index++)
        {
            var city = cities[index];
            var endDay = nextDay + city.Days - 1;
            var isLastCity = index == cities.Count - 1;
            var nights = isLastCity
                ? Math.Max(0, city.Days - 1)
                : city.Days;

            stops.Add(new ItineraryCityStop(
                city.Name,
                nextDay,
                endDay,
                nights,
                city.AirportCode,
                city.AirportName));

            nextDay = endDay + 1;
        }

        return stops;
    }

    private static bool IsCountryDestination(
        IReadOnlyList<PlaceDetails> results)
    {
        return results.FirstOrDefault()?.Types.Any(type =>
            string.Equals(
                type,
                "country",
                StringComparison.OrdinalIgnoreCase)) == true;
    }

    private static string GetSingleCityName(
        string destination,
        IReadOnlyList<PlaceDetails> results)
    {
        var locality = results.FirstOrDefault(place =>
            IsCityOrRegion(place.Types));

        return string.IsNullOrWhiteSpace(locality?.Name)
            ? destination.Trim()
            : locality.Name.Trim();
    }

    private static bool IsCityOrRegion(IEnumerable<string> types)
    {
        return types.Any(type =>
            type.Contains(
                "locality",
                StringComparison.OrdinalIgnoreCase) ||
            type.Contains(
                "administrative_area",
                StringComparison.OrdinalIgnoreCase));
    }

    private static int GetTripDayCount(Trip trip) =>
        Math.Max(
            1,
            (trip.EndDate.Date - trip.StartDate.Date).Days + 1);

    private sealed record ProposedCity(
        string Name,
        int Days,
        string? AirportCode,
        string? AirportName);
}
