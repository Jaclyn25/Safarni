using MediatR;
using Safarni.Application.Common.Results;
using Safarni.Application.Features.Trips.GetTrip;

namespace Safarni.Application.Features.Trips.PublishTrip
{
    public sealed record GetPublishedTripsQuery : IRequest<Result<IReadOnlyCollection<PublishedTripResponse>>>;

    public sealed record PublishedTripResponse(
        Guid Id,
        string Name,
        string Destination,
        DateTime StartDate,
        DateTime EndDate,
        string Status,
        bool IsPublished,
        DateTime? PublishedAt,
        string Visibility,
        Guid? SnapshotVersionId,
        int ViewsCount,
        int LikesCount,
        int SavesCount,
        int CopiesCount
    );
}