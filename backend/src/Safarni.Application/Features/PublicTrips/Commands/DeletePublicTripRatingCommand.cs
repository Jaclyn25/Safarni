using MediatR;
using Safarni.Application.Common.Results;
using System;

namespace Safarni.Application.Features.PublicTrips.Commands
{
    public sealed record DeletePublicTripRatingCommand(Guid VersionId) : IRequest<Result<DeletePublicTripRatingResult>>;

    public sealed record DeletePublicTripRatingResult(Guid VersionId, double NewAverageRating, int NewRatingsCount);
}
