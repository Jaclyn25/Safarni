using MediatR;
using Safarni.Application.Common.Results;

namespace Safarni.Application.Features.Trips.UpdateTrip;

public sealed record UpdateTripCommand(
    Guid TripId,
    string Name,
    string Destination,
    DateTime StartDate,
    DateTime EndDate,
    string OriginCountry,
    IReadOnlyList<TripCitySelection> Cities,
    string? OriginCountryCode = null,
    string? OriginCity = null,
    string? OriginAirportCode = null,
    string? OriginAirportName = null)
    : IRequest<Result<TripResponse>>;
