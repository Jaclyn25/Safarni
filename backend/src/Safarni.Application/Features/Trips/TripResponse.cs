using Safarni.Domain.Entities;
using Safarni.Domain.Enums;

namespace Safarni.Application.Features.Trips;

public sealed record TripResponse(
    Guid Id,
    Guid UserId,
    string Name,
    string Destination,
    string OriginCountry,
    string? OriginCountryCode,
    string? OriginCity,
    string? OriginAirportCode,
    string? OriginAirportName,
    IReadOnlyList<TripCitySelection> Cities,
    string Status,
    bool IsPublished,
    DateTime? PublishedAt,
    TripVisibility Visibility,
    DateTime StartDate,
    DateTime EndDate,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    Guid? ActiveItineraryId = null,
    string? ItineraryStatus = null,
    string? ActiveItineraryCurrencyCode = null,
    decimal? EstimatedTotalCost = null,
    DateTime? ActiveItineraryUpdatedAt = null);

internal static class TripResponseMapper
{
    public static TripResponse Map(Trip trip)
    {
        return TripResponseFactory.Create(trip);
    }
}
