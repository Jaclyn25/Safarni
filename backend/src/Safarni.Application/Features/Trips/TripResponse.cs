namespace Safarni.Application.Features.Trips;

public sealed record TripResponse(
    Guid Id,
    Guid UserId,
    string Name,
    string Destination,
    string OriginCountry,
    IReadOnlyList<TripCitySelection> Cities,
    DateTime StartDate,
    DateTime EndDate,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
