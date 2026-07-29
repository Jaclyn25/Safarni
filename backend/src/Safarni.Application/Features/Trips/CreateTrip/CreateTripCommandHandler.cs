using MediatR;
using Safarni.Application.Abstractions.Authentication;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;
using Safarni.Domain.Entities;

namespace Safarni.Application.Features.Trips.CreateTrip;

public sealed class CreateTripCommandHandler : IRequestHandler<CreateTripCommand, Result<TripResponse>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ITripRepository _tripRepository;

    public CreateTripCommandHandler(
        ICurrentUserService currentUserService,
        ITripRepository tripRepository)
    {
        _currentUserService = currentUserService;
        _tripRepository = tripRepository;
    }

    public async Task<Result<TripResponse>> Handle(CreateTripCommand request, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is not { } userId)
        {
            return Result<TripResponse>.Failure(AuthenticationErrors.AuthenticationRequired);
        }

        var trip = new Trip
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = request.Name.Trim(),
            Destination = request.Destination.Trim(),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            CreatedAt = DateTime.UtcNow
        };

        await _tripRepository.AddAsync(trip, cancellationToken);
        await _tripRepository.SaveAsync(cancellationToken);

        var response = new TripResponse(
            trip.Id,
            trip.UserId,
            trip.Name,
            trip.Destination,
            trip.StartDate,
            trip.EndDate,
            trip.CreatedAt,
            trip.UpdatedAt);

        return Result<TripResponse>.Success(response);
    }
}
