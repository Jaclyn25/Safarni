using ChatBotApi.Application.Common.Interfaces;
using ChatBotApi.Application.Common.Models.GooglePlaces;
using Safarni.Domain.Entities;

namespace Safarni.Application.Features.Trips.GenerateItinerary;

public sealed record ItineraryCitySelection(
    string Name,
    int Days);

internal sealed record ItineraryCityStop(
    string City,
    int StartDayNumber,
    int EndDayNumber)
{
    public int DayCount =>
        EndDayNumber - StartDayNumber + 1;

    public DateTime GetStartDate(Trip trip) =>
        trip.StartDate.Date.AddDays(StartDayNumber - 1);

    public DateTime GetEndDate(Trip trip) =>
        trip.StartDate.Date.AddDays(EndDayNumber - 1);
}

internal static class ItineraryCityPlanner
{
    public static async Task<IReadOnlyList<ItineraryCityStop>>
        ResolveAsync(
            Trip trip,
            IGooglePlacesService googlePlacesService,
            IReadOnlyList<ItineraryCitySelection>? selectedCities,
            CancellationToken cancellationToken)
    {
        if (selectedCities is { Count: > 0 })
        {
            var acceptedCities = selectedCities
                .Where(city =>
                    !string.IsNullOrWhiteSpace(city.Name))
                .Select(city =>
                    new ProposedCity(
                        city.Name.Trim(),
                        Math.Max(1, city.Days)))
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

        var destinationResults =
            await googlePlacesService.TextSearchAsync(
                trip.Destination,
                cancellationToken,
                "en");

        if (destinationResults.Count > 0 &&
            !IsCountryDestination(destinationResults))
        {
            return
            [
                new ItineraryCityStop(
                    GetSingleCityName(
                        trip.Destination,
                        destinationResults),
                    1,
                    GetTripDayCount(trip))
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
                $"{stop.GetEndDate(trip):yyyy-MM-dd})"));
    }

    private static bool IsCountryDestination(
        IReadOnlyList<PlaceDetails> results)
    {
        var bestResult = results.FirstOrDefault();

        return bestResult?.Types.Any(type =>
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

        foreach (var city in cities)
        {
            var endDay = nextDay + city.Days - 1;

            stops.Add(
                new ItineraryCityStop(
                    city.Name,
                    nextDay,
                    endDay));

            nextDay = endDay + 1;
        }

        return stops;
    }

    private static bool IsCityOrRegion(
        IEnumerable<string> types)
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
        int Days);
}
