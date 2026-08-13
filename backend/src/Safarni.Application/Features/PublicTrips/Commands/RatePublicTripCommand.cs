using MediatR;
using Safarni.Application.Common.Results;
using System;

namespace Safarni.Application.Features.PublicTrips.Commands
{
    public sealed record RatePublicTripCommand(
        Guid VersionId,
        int Score,
        string? Comment
    ) : IRequest<Result<RatePublicTripResult>>;

    public sealed record RatePublicTripResult(
        Guid VersionId,
        double NewAverageRating,
        int NewRatingsCount
    );
}