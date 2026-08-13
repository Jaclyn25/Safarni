using MediatR;
using Safarni.Application.Common.Results;
using Safarni.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Safarni.Application.Features.Trips.PublishTrip
{
public sealed record PublishTripCommand(
     Guid TripId,
     bool ConfirmedNoPersonalInfo,
     bool CostShown,
     decimal? CostAmount,
     List<string> IncludedPhotoUrls,
     TripVisibility Visibility) : IRequest<Result<PublishTripResult>>;

    public sealed record PublishTripResult(
        Guid TripId,
        Guid SnapshotId,
        Guid SnapshotVersionId,
        int VersionNumber,
        DateTime PublishedAt);


    public sealed record SetTripVisibilityCommand(
        Guid TripId,
        TripVisibility Visibility) : IRequest<Result<SetTripVisibilityResult>>;

    public sealed record SetTripVisibilityResult(
        Guid TripId,
        TripVisibility Visibility);
}