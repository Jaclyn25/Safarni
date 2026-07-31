using MediatR;
using Safarni.Application.Abstractions.Authentication;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;

namespace Safarni.Application.Features.Trips.ListTrips;

public sealed class ListTripsQueryHandler : IRequestHandler<ListTripsQuery, Result<IReadOnlyCollection<TripResponse>>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ITripRepository _tripRepository;

    public ListTripsQueryHandler(
        ICurrentUserService currentUserService,
        ITripRepository tripRepository)
    {
        _currentUserService = currentUserService;
        _tripRepository = tripRepository;
    }

    public async Task<Result<IReadOnlyCollection<TripResponse>>> Handle(
        ListTripsQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is not { } userId)
        {
            return Result<IReadOnlyCollection<TripResponse>>.Failure(
                AuthenticationErrors.AuthenticationRequired);
        }

        var trips = await _tripRepository.ListByUserIdAsync(
            userId,
            cancellationToken);

        return Result<IReadOnlyCollection<TripResponse>>.Success(
            trips.Select(TripResponseFactory.Create).ToArray());
    }
}
