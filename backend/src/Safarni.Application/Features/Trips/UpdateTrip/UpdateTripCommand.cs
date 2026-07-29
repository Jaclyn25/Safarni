using MediatR;
using Safarni.Application.Common.Results;

namespace Safarni.Application.Features.Trips.UpdateTrip;

public sealed record UpdateTripCommand(
    Guid TripId,
    string Name,
    string Destination,
    DateTime StartDate,
    DateTime EndDate) : IRequest<Result<TripResponse>>;
