using MediatR;
using Safarni.Application.Common.Results;

namespace Safarni.Application.Features.Trips.GetTrip;

public sealed record GetTripQuery(Guid TripId) : IRequest<Result<TripResponse>>;
