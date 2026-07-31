using MediatR;
using Safarni.Application.Abstractions.Authentication;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;

namespace Safarni.Application.Features.Trips.GetTrip;

public sealed class GetTripQueryHandler : IRequestHandler<GetTripQuery, Result<TripResponse>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ITripRepository _tripRepository;

    public GetTripQueryHandler(
        ICurrentUserService currentUserService,
        ITripRepository tripRepository)
    {
        _currentUserService = currentUserService;
        _tripRepository = tripRepository;
    }

    public async Task<Result<TripResponse>> Handle(
        GetTripQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is not { } userId)
        {
            return Result<TripResponse>.Failure(
                AuthenticationErrors.AuthenticationRequired);
        }

        var trip = await _tripRepository.GetByIdWithItinerariesAsync(
            request.TripId,
            cancellationToken);

        if (trip is null)
        {
            return Result<TripResponse>.Failure(TripErrors.NotFound);
        }

        if (trip.UserId != userId)
        {
            return Result<TripResponse>.Failure(new Error(
                "Trip.Forbidden",
                "You do not have permission to view this trip.",
                ErrorType.Forbidden));
        }

        return Result<TripResponse>.Success(
            TripResponseFactory.Create(trip));
    }
}
