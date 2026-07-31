using MediatR;
using Safarni.Application.Abstractions.Authentication;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;
using Safarni.Domain.Entities;

namespace Safarni.Application.Features.Trips.UpdateTrip;

public sealed class UpdateTripCommandHandler : IRequestHandler<UpdateTripCommand, Result<TripResponse>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ITripRepository _tripRepository;

    public UpdateTripCommandHandler(
        ICurrentUserService currentUserService,
        ITripRepository tripRepository)
    {
        _currentUserService = currentUserService;
        _tripRepository = tripRepository;
    }

    public async Task<Result<TripResponse>> Handle(UpdateTripCommand request, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is not { } userId)
        {
            return Result<TripResponse>.Failure(AuthenticationErrors.AuthenticationRequired);
        }

        var trip = await _tripRepository.GetByIdAsync(request.TripId, cancellationToken);
        if (trip is null)
        {
            return Result<TripResponse>.Failure(new Error("Trip.NotFound", "The requested trip was not found.", ErrorType.NotFound));
        }

        if (trip.UserId != userId)
        {
            return Result<TripResponse>.Failure(new Error("Trip.Forbidden", "You do not have permission to modify this trip.", ErrorType.Forbidden));
        }

        trip.Name = request.Name.Trim();
        trip.Destination = request.Destination.Trim();
        trip.OriginCountry = request.OriginCountry.Trim();
        trip.SelectedCitiesJson =
            TripRouteSerializer.Serialize(
                request.Cities
                    .Select(city =>
                        new TripCitySelection(
                            city.Name.Trim(),
                            city.Days))
                    .ToList());
        trip.StartDate = request.StartDate;
        trip.EndDate = request.EndDate;
        trip.UpdatedAt = DateTime.UtcNow;

        _tripRepository.Update(trip);
        await _tripRepository.SaveAsync(cancellationToken);

        var response = new TripResponse(
            trip.Id,
            trip.UserId,
            trip.Name,
            trip.Destination,
            trip.OriginCountry,
            TripRouteSerializer.Deserialize(
                trip.SelectedCitiesJson),
            trip.StartDate,
            trip.EndDate,
            trip.CreatedAt,
            trip.UpdatedAt);

        return Result<TripResponse>.Success(response);
    }
}
