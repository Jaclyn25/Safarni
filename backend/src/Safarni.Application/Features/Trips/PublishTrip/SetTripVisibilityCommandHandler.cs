using MediatR;
using Safarni.Application.Abstractions.Authentication;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;

namespace Safarni.Application.Features.Trips.PublishTrip
{
    public sealed class SetTripVisibilityCommandHandler
        : IRequestHandler<SetTripVisibilityCommand, Result<SetTripVisibilityResult>>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly ITripRepository _tripRepository;

        public SetTripVisibilityCommandHandler(
            ICurrentUserService currentUserService,
            ITripRepository tripRepository)
        {
            _currentUserService = currentUserService;
            _tripRepository = tripRepository;
        }

        public async Task<Result<SetTripVisibilityResult>> Handle(
            SetTripVisibilityCommand request,
            CancellationToken cancellationToken)
        {
            if (_currentUserService.UserId is not { } userId)
            {
                return Result<SetTripVisibilityResult>.Failure(
                    AuthenticationErrors.AuthenticationRequired);
            }

            var trip = await _tripRepository.GetByIdAsync(request.TripId, cancellationToken);
            if (trip is null)
            {
                return Result<SetTripVisibilityResult>.Failure(TripErrors.NotFound);
            }

            if (trip.UserId != userId)
            {
                return Result<SetTripVisibilityResult>.Failure(TripErrors.Forbidden);
            }

            if (!trip.IsPublished)
            {
                return Result<SetTripVisibilityResult>.Failure(TripErrors.NotCompleted);
            }

            trip.Visibility = request.Visibility;

            await _tripRepository.SaveAsync(cancellationToken);

            return Result<SetTripVisibilityResult>.Success(
                new SetTripVisibilityResult(trip.Id, trip.Visibility));
        }
    }
}