using Safarni.Application.Abstractions.Travel;
using Safarni.Domain.Entities;

namespace Safarni.Application.Features.Trips.GenerateItinerary;

internal static class ItineraryRouteEnricher
{
    public static async Task EnrichAsync(
        Itinerary itinerary,
        GenerateItineraryPreferences preferences,
        IItineraryRouteService? routeService,
        CancellationToken cancellationToken)
    {
        if (routeService?.IsConfigured != true)
        {
            return;
        }

        ItineraryItem? previousPlace = null;

        foreach (var item in itinerary.Days
                     .OrderBy(day => day.DayNumber)
                     .SelectMany(day => day.Items
                         .OrderBy(candidate =>
                             candidate.OrderIndex)))
        {
            if (item.Latitude is null ||
                item.Longitude is null)
            {
                continue;
            }

            if (previousPlace?.Latitude is not null &&
                previousPlace.Longitude is not null)
            {
                var route =
                    await routeService.ComputeRouteAsync(
                        new ItineraryRoutePoint(
                            previousPlace.Latitude.Value,
                            previousPlace.Longitude.Value),
                        new ItineraryRoutePoint(
                            item.Latitude.Value,
                            item.Longitude.Value),
                        preferences.Transportation,
                        preferences.PreferredLanguage,
                        cancellationToken);

                if (route is not null)
                {
                    item.RouteDistanceMeters =
                        route.DistanceMeters;
                    item.RouteDurationMinutes =
                        route.DurationMinutes;
                    item.RouteTravelMode =
                        route.TravelMode;
                    item.RouteTransitDetails =
                        route.TransitDetails;
                }
            }

            previousPlace = item;
        }
    }
}
