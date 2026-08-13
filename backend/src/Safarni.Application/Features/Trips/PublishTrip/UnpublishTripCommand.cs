using MediatR;
using Safarni.Application.Common.Results;
using System;

namespace Safarni.Application.Features.Trips.PublishTrip
{
    public sealed record UnpublishTripCommand(Guid TripId) : IRequest<Result<UnpublishTripResult>>;

    public sealed record UnpublishTripResult(Guid TripId);
}