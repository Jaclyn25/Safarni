using Safarni.Application.Abstractions.Travel;
using Safarni.Domain.Entities;

namespace Safarni.Application.Features.Trips.GenerateItinerary;

internal static class ItineraryRouteEnricher
{
    public static Task EnrichAsync(
        Itinerary itinerary,
        GenerateItineraryPreferences preferences,
        IItineraryRouteService? routeService,
        CancellationToken cancellationToken)
    {
        return EnrichDaysAsync(
            itinerary.Days,
            preferences,
            routeService,
            cancellationToken);
    }

    /// <summary>
    /// Rebuilds routes independently for every supplied day. A route never
    /// crosses a day boundary and transport items (flights/intercity transfers)
    /// reset the local route chain.
    /// </summary>
    public static async Task EnrichDaysAsync(
        IEnumerable<ItineraryDay> days,
        GenerateItineraryPreferences preferences,
        IItineraryRouteService? routeService,
        CancellationToken cancellationToken)
    {
        var orderedDays = days
            .DistinctBy(day => day.Id)
            .OrderBy(day => day.DayNumber)
            .ToList();

        await Parallel.ForEachAsync(
            orderedDays,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = 3,
                CancellationToken = cancellationToken
            },
            async (day, token) =>
            {
                await EnrichDayAsync(
                    day,
                    preferences,
                    routeService,
                    token);
            });
    }

    public static async Task EnrichDayAsync(
        ItineraryDay day,
        GenerateItineraryPreferences preferences,
        IItineraryRouteService? routeService,
        CancellationToken cancellationToken)
    {
        var orderedItems = day.Items
            .OrderBy(item => item.OrderIndex)
            .ToList();

        foreach (var item in orderedItems)
        {
            ClearRoute(item);
        }

        if (routeService?.IsConfigured != true)
        {
            return;
        }

        ItineraryItem? previousPlace = null;

        foreach (var item in orderedItems)
        {
            // Flights and explicit intercity/local transport items are route
            // boundaries. They must never connect places from different cities.
            if (item.Category == ItineraryItemCategory.Transport)
            {
                previousPlace = null;
                continue;
            }

            if (!HasCoordinates(item))
            {
                continue;
            }

            if (previousPlace is not null)
            {
                var route = await routeService.ComputeRouteAsync(
                    new ItineraryRoutePoint(
                        previousPlace.Latitude!.Value,
                        previousPlace.Longitude!.Value),
                    new ItineraryRoutePoint(
                        item.Latitude!.Value,
                        item.Longitude!.Value),
                    preferences.Transportation,
                    preferences.PreferredLanguage,
                    cancellationToken);

                if (route is not null)
                {
                    item.RouteDistanceMeters = route.DistanceMeters;
                    item.RouteDurationMinutes = route.DurationMinutes;
                    item.RouteTravelMode = route.TravelMode;
                    item.RouteTransitDetails = route.TransitDetails;
                }
            }

            previousPlace = item;
        }
    }

    private static bool HasCoordinates(ItineraryItem item) =>
        item.Latitude is not null && item.Longitude is not null;

    private static void ClearRoute(ItineraryItem item)
    {
        item.RouteDistanceMeters = null;
        item.RouteDurationMinutes = null;
        item.RouteTravelMode = null;
        item.RouteTransitDetails = null;
    }
}
