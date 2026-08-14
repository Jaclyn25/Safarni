using MediatR;
using Safarni.Application.Abstractions.Authentication;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;
using Safarni.Domain.Enums;
using System.Linq;
using System.Text.Json;

namespace Safarni.Application.Features.PublicTrips.Queries
{
    public sealed class GetPublicTripDetailsQueryHandler
        : IRequestHandler<GetPublicTripDetailsQuery, Result<PublicTripDetailsResponse>>
    {
        private readonly IPublicTripRepository _repository;
        private readonly ICurrentUserService _currentUserService;

        public GetPublicTripDetailsQueryHandler(
            IPublicTripRepository repository,
            ICurrentUserService currentUserService)
        {
            _repository = repository;
            _currentUserService = currentUserService;
        }

        public async Task<Result<PublicTripDetailsResponse>> Handle(
            GetPublicTripDetailsQuery request,
            CancellationToken cancellationToken)
        {
            var version = await _repository.GetSnapshotVersionWithTripAndUserAsync(request.VersionId, cancellationToken);

            if (version == null)
            {
                return Result<PublicTripDetailsResponse>.Failure(
                    new Error("PublicTrip.NotFound", "Public trip version not found.", ErrorType.NotFound));
            }

            if (!version.IsActive)
            {
                return Result<PublicTripDetailsResponse>.Failure(
                    new Error("PublicTrip.NotActive", "This snapshot version is no longer active.", ErrorType.Validation));
            }

            var trip = version.Snapshot.Trip;
            var user = trip.User;

            // Increment ViewsCount
            version.ViewsCount++;
            await _repository.SaveChangesAsync(cancellationToken);

            var isUserDeleted = !user.IsActive;
            var authorProfile = new AuthorProfileDto(
                user.Id,
                isUserDeleted ? "Deleted User" : user.FullName,
                isUserDeleted ? string.Empty : (user.ProfilePhotoUrl ?? string.Empty),
                isUserDeleted
            );

            bool isLikedByCurrentUser = false;
            bool isSavedByCurrentUser = false;
            UserRatingDto? userRatingDto = null;

            if (_currentUserService.UserId is { } currentUserId)
            {
                isLikedByCurrentUser = version.Interactions
                    .Any(i => i.UserId == currentUserId && i.Type == TripInteractionType.Like);
                isSavedByCurrentUser = version.Interactions
                    .Any(i => i.UserId == currentUserId && i.Type == TripInteractionType.Save);

                var rating = version.Ratings.FirstOrDefault(r => r.UserId == currentUserId);
                if (rating != null)
                {
                    userRatingDto = new UserRatingDto(rating.Score, rating.Comment, rating.CreatedAt);
                }
            }

            return Result<PublicTripDetailsResponse>.Success(new PublicTripDetailsResponse(
                version.Id,
                trip.Id,
                trip.Name,
                version.ItineraryJson,
                version.CostShown,
                version.CostAmount,
                version.IncludedPhotoIdsJson,
                version.ViewsCount,
                version.LikesCount,
                version.SavesCount,
                version.CopiesCount,
                version.RatingsCount,
                version.AverageRating,
                authorProfile,
                isLikedByCurrentUser,
                isSavedByCurrentUser,
                userRatingDto
            ));
        }
    }
}