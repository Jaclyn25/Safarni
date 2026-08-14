using MediatR;
using Safarni.Application.Common.Results;
using System;

namespace Safarni.Application.Features.PublicTrips.Commands
{
    public sealed record CopyPublicTripCommand(Guid VersionId) : IRequest<Result<CopyPublicTripResult>>;

    public sealed record CopyPublicTripResult(Guid NewTripId);
}