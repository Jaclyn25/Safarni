using MediatR;
using Safarni.Application.Abstractions.Authentication;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;
using Safarni.Application.Features.Trips.GenerateItinerary;

namespace Safarni.Application.Features.Trips.Itineraries.GetActiveItinerary;

public sealed record GetActiveItineraryQuery(
    Guid TripId) : IRequest<Result<ItineraryResponse>>;

public sealed class GetActiveItineraryQueryHandler
    : IRequestHandler<GetActiveItineraryQuery, Result<ItineraryResponse>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ITripRepository _tripRepository;
    private readonly IItineraryRepository _itineraryRepository;

    public GetActiveItineraryQueryHandler(
        ICurrentUserService currentUserService,
        ITripRepository tripRepository,
        IItineraryRepository itineraryRepository)
    {
        _currentUserService = currentUserService;
        _tripRepository = tripRepository;
        _itineraryRepository = itineraryRepository;
    }

    public async Task<Result<ItineraryResponse>> Handle(
        GetActiveItineraryQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
        {
            return Result<ItineraryResponse>.Failure(
                AuthenticationErrors.AuthenticationRequired);
        }

        var trip = await _tripRepository.GetByIdAsync(
            request.TripId,
            cancellationToken);

        if (trip is null)
        {
            return Result<ItineraryResponse>.Failure(
                ItineraryErrors.TripNotFound);
        }

        if (trip.UserId != userId.Value)
        {
            return Result<ItineraryResponse>.Failure(
                ItineraryErrors.Forbidden);
        }

        var itinerary = await _itineraryRepository.GetActiveByTripIdAsync(
            request.TripId,
            cancellationToken);

        return itinerary is null
            ? Result<ItineraryResponse>.Failure(ItineraryErrors.NotFound)
            : Result<ItineraryResponse>.Success(
                ItineraryResponseMapper.Map(itinerary));
    }
}
