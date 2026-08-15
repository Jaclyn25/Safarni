using MediatR;
using Safarni.Application.Abstractions.Authentication;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;
using Safarni.Domain.Entities;

namespace Safarni.Application.Features.PublicTrips.Commands
{
    public sealed class RatePublicTripCommandHandler
        : IRequestHandler<RatePublicTripCommand, Result<RatePublicTripResult>>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IPublicTripRepository _repository;

        public RatePublicTripCommandHandler(
            ICurrentUserService currentUserService,
            IPublicTripRepository repository)
        {
            _currentUserService = currentUserService;
            _repository = repository;
        }

        public async Task<Result<RatePublicTripResult>> Handle(
            RatePublicTripCommand request,
            CancellationToken cancellationToken)
        {
            if (_currentUserService.UserId is not { } userId)
            {
                return Result<RatePublicTripResult>.Failure(
                    AuthenticationErrors.AuthenticationRequired);
            }

            if (request.Score < 1 || request.Score > 5)
            {
                return Result<RatePublicTripResult>.Failure(
                    new Error("PublicTrip.InvalidScore", "Rating score must be between 1 and 5.", ErrorType.Validation));
            }

            var version = await _repository.GetSnapshotVersionWithTripAndUserAsync(request.VersionId, cancellationToken);

            if (version == null)
            {
                return Result<RatePublicTripResult>.Failure(
                    new Error("PublicTrip.NotFound", "Public trip version not found.", ErrorType.NotFound));
            }

            if (!version.IsActive)
            {
                return Result<RatePublicTripResult>.Failure(
                    new Error("PublicTrip.NotActive", "This snapshot version is no longer active.", ErrorType.Validation));
            }

            // TODO: Uncomment this in production to restrict rating to completed trips only.
            // Temporarily disabled for testing purposes.
            /*
            if (DateTime.UtcNow.Date < version.Snapshot.Trip.EndDate.Date)
            {
                return Result<RatePublicTripResult>.Failure(
                    new Error(
                        "PublicTrip.RatingNotOpenYet",
                        "You can rate this trip after it ends.",
                        ErrorType.Validation));
            }
            */

            var existingRating = version.Ratings.FirstOrDefault(r => r.UserId == userId);

            if (existingRating != null)
            {
                existingRating.Score = request.Score;
                existingRating.Comment = request.Comment;
                existingRating.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                var newRating = new TripRating
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    SnapshotVersionId = version.Id,
                    Score = request.Score,
                    Comment = request.Comment,
                    CreatedAt = DateTime.UtcNow
                };
                await _repository.AddTripRatingAsync(newRating, cancellationToken);
                version.Ratings.Add(newRating);
                version.RatingsCount++;
            }

            // Recalculate average
            if (version.Ratings.Any())
            {
                version.AverageRating = version.Ratings.Average(r => r.Score);
            }
            else
            {
                version.AverageRating = 0;
            }

            await _repository.SaveChangesAsync(cancellationToken);

            return Result<RatePublicTripResult>.Success(
                new RatePublicTripResult(version.Id, version.AverageRating, version.RatingsCount));
        }
    }
}