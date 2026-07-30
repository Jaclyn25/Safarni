using MediatR;
using Safarni.Application.Abstractions.Authentication;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;

namespace Safarni.Application.Features.Trips.DeleteTrip;

public sealed class DeleteTripCommandHandler: IRequestHandler<DeleteTripCommand, Result>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ITripRepository _tripRepository;

    public DeleteTripCommandHandler(
        ICurrentUserService currentUserService,
        ITripRepository tripRepository)
    {
        _currentUserService = currentUserService;
        _tripRepository = tripRepository;
    }

    public async Task<Result> Handle(
        DeleteTripCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Check if the user is logged in
        if (_currentUserService.UserId is not { } userId)
        {
            return Result.Failure(AuthenticationErrors.AuthenticationRequired);
        }

        // 2. Get the trip from the database
        var trip = await _tripRepository.GetByIdAsync(
            request.Id,
            cancellationToken);

        // 3. Check if the trip exists
        if (trip is null)
        {
            return Result.Failure(TripErrors.NotFound);
        }

        // 4. Make sure the trip belongs to the current user
        if (trip.UserId != userId)
        {
            return Result.Failure(TripErrors.NotFound);
        }

        // 5. Remove the trip
        _tripRepository.Remove(trip);

        // 6. Save changes
        await _tripRepository.SaveAsync(cancellationToken);

        // 7. Return success
        return Result.Success();
    }
}