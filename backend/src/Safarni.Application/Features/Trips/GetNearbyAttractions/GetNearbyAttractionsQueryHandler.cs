using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Safarni.Application.Abstractions.Authentication;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Abstractions.Travel;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;

namespace Safarni.Application.Features.Trips.GetNearbyAttractions;

public sealed class GetNearbyAttractionsQueryHandler : IRequestHandler<GetNearbyAttractionsQuery, Result<List<NearbyAttractionDto>>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ITripRepository _tripRepository;
    private readonly IGeoapifyService _geoapifyService;

    public GetNearbyAttractionsQueryHandler(
        ICurrentUserService currentUserService,
        ITripRepository tripRepository,
        IGeoapifyService geoapifyService)
    {
        _currentUserService = currentUserService;
        _tripRepository = tripRepository;
        _geoapifyService = geoapifyService;
    }

    public async Task<Result<List<NearbyAttractionDto>>> Handle(
        GetNearbyAttractionsQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is not { } userId)
        {
            return Result<List<NearbyAttractionDto>>.Failure(AuthenticationErrors.AuthenticationRequired);
        }

        var trip = await _tripRepository.GetByIdWithItinerariesAsync(request.TripId, cancellationToken);
        if (trip is null)
        {
            return Result<List<NearbyAttractionDto>>.Failure(TripErrors.NotFound);
        }

        if (trip.UserId != userId)
        {
            return Result<List<NearbyAttractionDto>>.Failure(new Error(
                "Trip.Forbidden",
                "You do not have permission to view this trip.",
                ErrorType.Forbidden));
        }

        // Determine the center location for the search.
        // We will try to find the first activity with latitude and longitude.
        var firstActivity = trip.Itineraries
            .SelectMany(i => i.Days)
            .OrderBy(d => d.Date)
            .SelectMany(d => d.Items)
            .FirstOrDefault(item => item.Latitude.HasValue && item.Longitude.HasValue);

        double latitude;
        double longitude;

        if (firstActivity != null && firstActivity.Latitude.HasValue && firstActivity.Longitude.HasValue)
        {
            latitude = firstActivity.Latitude.Value;
            longitude = firstActivity.Longitude.Value;
        }
        else
        {
            // If no activity has coordinates, fallback to a default logic or return error
            // For now, we return a validation error as we don't have Geocoding implemented for cities yet
            return Result<List<NearbyAttractionDto>>.Failure(new Error(
                "Trip.NoLocation",
                "Cannot determine the location of the trip to find nearby attractions.",
                ErrorType.Validation));
        }

        var result = await _geoapifyService.GetNearbyPlacesAsync(
            latitude,
            longitude,
            request.Radius,
            request.Category,
            cancellationToken);

        return result;
    }
}
