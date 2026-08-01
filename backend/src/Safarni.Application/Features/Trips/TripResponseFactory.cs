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

        decimal? estimatedTotalCost = activeItinerary is null
            ? null
            : activeItinerary.Days
                .SelectMany(day => day.Items)
                .Sum(item => item.EstimatedCost ?? 0m);

        return new TripResponse(
            Id: trip.Id,
            UserId: trip.UserId,
            Name: trip.Name,
            Destination: trip.Destination,
            OriginCountry: trip.OriginCountry,
            OriginCountryCode: trip.OriginCountryCode,
            OriginCity: trip.OriginCity,
            OriginAirportCode: trip.OriginAirportCode,
            OriginAirportName: trip.OriginAirportName,
            Cities: TripRouteSerializer.ReadFromTrip(trip),
            StartDate: trip.StartDate,
            EndDate: trip.EndDate,
            CreatedAt: trip.CreatedAt,
            UpdatedAt: trip.UpdatedAt,
            ActiveItineraryId: activeItinerary?.Id,
            ItineraryStatus: activeItinerary?.Status
                .ToString()
                .ToLowerInvariant(),
            ActiveItineraryCurrencyCode: activeItinerary?.CurrencyCode,
            EstimatedTotalCost: estimatedTotalCost,
            ActiveItineraryUpdatedAt: activeItinerary?.UpdatedAt);
    }
}
