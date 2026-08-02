using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Safarni.Application.Abstractions.Authentication;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;
using Safarni.Domain.Entities;

namespace Safarni.Application.Features.Trips.GetTripRoute;

public sealed class GetTripRouteQueryHandler : IRequestHandler<GetTripRouteQuery, Result<TripRouteResponse>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ITripRepository _tripRepository;

    public GetTripRouteQueryHandler(
        ICurrentUserService currentUserService,
        ITripRepository tripRepository)
    {
        _currentUserService = currentUserService;
        _tripRepository = tripRepository;
    }

    public async Task<Result<TripRouteResponse>> Handle(
        GetTripRouteQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is not { } userId)
        {
            return Result<TripRouteResponse>.Failure(AuthenticationErrors.AuthenticationRequired);
        }

        var trip = await _tripRepository.GetByIdWithItinerariesAsync(request.TripId, cancellationToken);
        if (trip is null)
        {
            return Result<TripRouteResponse>.Failure(TripErrors.NotFound);
        }

        if (trip.UserId != userId)
        {
            return Result<TripRouteResponse>.Failure(new Error(
                "Trip.Forbidden",
                "You do not have permission to view this trip.",
                ErrorType.Forbidden));
        }

        var activities = trip.Itineraries
            .SelectMany(i => i.Days)
            .OrderBy(d => d.Date)
            .SelectMany(d => d.Items)
            .Where(item => item.Category == ItineraryItemCategory.Activity || (item.Latitude.HasValue && item.Longitude.HasValue))
            .OrderBy(item => item.ItineraryDay.Date)
            .ThenBy(item => item.OrderIndex)
            .Select(item => new TripRouteActivityDto
            {
                ActivityId = item.Id,
                Name = item.Title,
                Address = item.Address,
                Latitude = item.Latitude,
                Longitude = item.Longitude,
                Order = item.OrderIndex,
                Description = item.Description
            })
            .ToList();

        var response = new TripRouteResponse
        {
            Activities = activities
        };

        return Result<TripRouteResponse>.Success(response);
    }
}
