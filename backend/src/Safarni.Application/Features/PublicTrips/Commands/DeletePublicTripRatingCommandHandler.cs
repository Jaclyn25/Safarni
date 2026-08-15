using MediatR;
using Safarni.Application.Abstractions.Authentication;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Safarni.Application.Features.PublicTrips.Commands
{
    public sealed class DeletePublicTripRatingCommandHandler
        : IRequestHandler<DeletePublicTripRatingCommand, Result<DeletePublicTripRatingResult>>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IPublicTripRepository _repository;

        public DeletePublicTripRatingCommandHandler(
            ICurrentUserService currentUserService,
            IPublicTripRepository repository)
        {
            _currentUserService = currentUserService;
            _repository = repository;
        }

        public async Task<Result<DeletePublicTripRatingResult>> Handle(
            DeletePublicTripRatingCommand request,
            CancellationToken cancellationToken)
        {
            if (_currentUserService.UserId is not { } userId)
            {
                return Result<DeletePublicTripRatingResult>.Failure(
                    AuthenticationErrors.AuthenticationRequired);
            }

            var version = await _repository.GetSnapshotVersionWithRatingsAsync(request.VersionId, cancellationToken);

            if (version == null)
            {
                return Result<DeletePublicTripRatingResult>.Failure(
                    new Error("PublicTrip.NotFound", "Public trip version not found.", ErrorType.NotFound));
            }

            if (!version.IsActive)
            {
                return Result<DeletePublicTripRatingResult>.Failure(
                    new Error("PublicTrip.NotActive", "This snapshot version is no longer active.", ErrorType.Validation));
            }

            var existingRating = version.Ratings.FirstOrDefault(r => r.UserId == userId);

            if (existingRating == null)
            {
                return Result<DeletePublicTripRatingResult>.Failure(
                    new Error("PublicTrip.RatingNotFound", "You have not rated this trip.", ErrorType.NotFound));
            }

            _repository.RemoveTripRating(existingRating);
            version.Ratings.Remove(existingRating);
            version.RatingsCount--;

            if (version.Ratings.Any())
            {
                version.AverageRating = version.Ratings.Average(r => r.Score);
            }
            else
            {
                version.AverageRating = 0;
            }

            await _repository.SaveChangesAsync(cancellationToken);

            return Result<DeletePublicTripRatingResult>.Success(
                new DeletePublicTripRatingResult(version.Id, version.AverageRating, version.RatingsCount));
        }
    }
}
