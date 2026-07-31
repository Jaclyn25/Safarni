using Safarni.Domain.Entities;

namespace Safarni.Application.Features.Trips;

internal static class TripResponseFactory
{
    public static TripResponse Create(Trip trip)
    {
        var activeItinerary = trip.Itineraries
            .Where(itinerary => itinerary.IsActive)
            .OrderByDescending(itinerary => itinerary.UpdatedAt)
            .FirstOrDefault();

        return new TripResponse(
            trip.Id,
            trip.UserId,
            trip.Name,
            trip.Destination,
            trip.OriginCountry,
            TripRouteSerializer.Deserialize(trip.SelectedCitiesJson),
            trip.StartDate,
            trip.EndDate,
            trip.CreatedAt,
            trip.UpdatedAt,
            activeItinerary?.Id,
            activeItinerary?.Status.ToString().ToLowerInvariant(),
            activeItinerary?.CurrencyCode,
            activeItinerary is null
                ? null
                : activeItinerary.Days.Sum(
                    day => day.Items.Sum(item => item.EstimatedCost ?? 0)),
            activeItinerary?.UpdatedAt);
    }
}
