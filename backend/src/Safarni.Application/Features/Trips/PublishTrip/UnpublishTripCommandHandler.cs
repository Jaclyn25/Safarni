using MediatR;
using Safarni.Application.Abstractions.Authentication;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;
using Safarni.Domain.Enums;

namespace Safarni.Application.Features.Trips.PublishTrip
{
    public sealed class UnpublishTripCommandHandler
        : IRequestHandler<UnpublishTripCommand, Result<UnpublishTripResult>>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly ITripRepository _tripRepository;

        public UnpublishTripCommandHandler(
            ICurrentUserService currentUserService,
            ITripRepository tripRepository)
        {
            _currentUserService = currentUserService;
            _tripRepository = tripRepository;
        }

        public async Task<Result<UnpublishTripResult>> Handle(
            UnpublishTripCommand request,
            CancellationToken cancellationToken)
        {
            if (_currentUserService.UserId is not { } userId)
            {
                return Result<UnpublishTripResult>.Failure(
                    AuthenticationErrors.AuthenticationRequired);
            }

            var trip = await _tripRepository.GetByIdAsync(request.TripId, cancellationToken);
            if (trip is null)
            {
                return Result<UnpublishTripResult>.Failure(TripErrors.NotFound);
            }

            if (trip.UserId != userId)
            {
                return Result<UnpublishTripResult>.Failure(TripErrors.Forbidden);
            }

            if (!trip.IsPublished)
            {
                // Already unpublished or not published yet
                return Result<UnpublishTripResult>.Success(new UnpublishTripResult(trip.Id));
            }

            trip.IsPublished = false;
            trip.Visibility = TripVisibility.Private;
            // Note: We do NOT delete the PublicTripSnapshot or SnapshotVersions per requirements.

            await _tripRepository.SaveAsync(cancellationToken);

            return Result<UnpublishTripResult>.Success(new UnpublishTripResult(trip.Id));
        }
    }
}