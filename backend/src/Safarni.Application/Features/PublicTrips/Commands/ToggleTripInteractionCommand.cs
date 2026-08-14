using MediatR;
using Safarni.Application.Common.Results;
using Safarni.Domain.Enums;
using System;

namespace Safarni.Application.Features.PublicTrips.Commands
{
    public sealed record ToggleTripInteractionCommand(
        Guid VersionId,
        TripInteractionType InteractionType
    ) : IRequest<Result<ToggleTripInteractionResult>>;

    public sealed record ToggleTripInteractionResult(
        Guid VersionId,
        int NewCount,
        bool IsActive
    );
}