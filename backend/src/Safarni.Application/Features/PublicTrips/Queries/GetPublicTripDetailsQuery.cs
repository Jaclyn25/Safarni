using MediatR;
using Safarni.Application.Common.Results;
using System;

namespace Safarni.Application.Features.PublicTrips.Queries
{
    public sealed record GetPublicTripDetailsQuery(Guid VersionId) : IRequest<Result<PublicTripDetailsResponse>>;

    public sealed record PublicTripDetailsResponse(
        Guid SnapshotVersionId,
        Guid TripId,
        string TripName,
        string ItineraryJson,
        bool CostShown,
        decimal? CostAmount,
        string IncludedPhotoIdsJson,
        int ViewsCount,
        int LikesCount,
        int SavesCount,
        int CopiesCount,
        int RatingsCount,
        double AverageRating,
        AuthorProfileDto Author,
        bool IsLikedByCurrentUser = false,
        bool IsSavedByCurrentUser = false,
        UserRatingDto? UserRating = null
    );

    public sealed record AuthorProfileDto(
        Guid UserId,
        string DisplayName,
        string ProfilePhotoUrl,
        bool IsDeleted
    );

    public sealed record UserRatingDto(
        int Score,
        string? Comment,
        DateTime CreatedAt
    );
}