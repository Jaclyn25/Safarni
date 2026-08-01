using MediatR;
using Safarni.Application.Abstractions.Authentication;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;

namespace Safarni.Application.Features.Trips.UpdateTrip;

public sealed class UpdateTripCommandHandler
    : IRequestHandler<UpdateTripCommand, Result<TripResponse>>
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

    public async Task<Result<TripResponse>> Handle(
        UpdateTripCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is not { } userId)
        {
            return Result<TripResponse>.Failure(
                AuthenticationErrors.AuthenticationRequired);
        }

        var trip = await _tripRepository.GetByIdAsync(
            request.TripId,
            cancellationToken);

        if (trip is null)
        {
            return Result<TripResponse>.Failure(TripErrors.NotFound);
        }

        if (trip.UserId != userId)
        {
            return Result<TripResponse>.Failure(TripErrors.Forbidden);
        }

        trip.Name = request.Name.Trim();
        trip.Destination = request.Destination.Trim();
        trip.OriginCountry = request.OriginCountry.Trim();
        trip.OriginCountryCode = NormalizeCountryCode(
            request.OriginCountryCode);
        trip.OriginCity = NullIfWhiteSpace(request.OriginCity);
        trip.OriginAirportCode = TripRouteSerializer.NormalizeAirportCode(
            request.OriginAirportCode);
        trip.OriginAirportName = NullIfWhiteSpace(
            request.OriginAirportName);
        trip.StartDate = request.StartDate.Date;
        trip.EndDate = request.EndDate.Date;
        trip.UpdatedAt = DateTime.UtcNow;

        TripRouteSerializer.ReplaceCities(trip, request.Cities);

        // The trip and its city collection were loaded as a tracked graph.
        // Calling Update here would mark newly added TripCity rows as Modified
        // because they already have GUID keys, which can produce UPDATEs for
        // rows that do not exist. Change tracking will correctly insert/delete
        // the normalized city rows without Update().
        await _tripRepository.SaveAsync(cancellationToken);

        return Result<TripResponse>.Success(
            TripResponseMapper.Map(trip));
    }

    private static string? NormalizeCountryCode(string? value)
    {
        var normalized = NullIfWhiteSpace(value)?.ToUpperInvariant();
        return normalized is { Length: 2 } && normalized.All(char.IsLetter)
            ? normalized
            : null;
    }

    private static string? NullIfWhiteSpace(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
