using MediatR;
using Safarni.Application.Abstractions.Authentication;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;
using Safarni.Domain.Entities;

namespace Safarni.Application.Features.Trips.CreateTrip;

public sealed class CreateTripCommandHandler
    : IRequestHandler<CreateTripCommand, Result<TripResponse>>
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

    public async Task<Result<TripResponse>> Handle(
        CreateTripCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is not { } userId)
        {
            return Result<TripResponse>.Failure(
                AuthenticationErrors.AuthenticationRequired);
        }

        var trip = new Trip
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = request.Name.Trim(),
            Destination = request.Destination.Trim(),
            OriginCountry = request.OriginCountry.Trim(),
            OriginCountryCode = NormalizeCountryCode(
                request.OriginCountryCode),
            OriginCity = NullIfWhiteSpace(request.OriginCity),
            OriginAirportCode = TripRouteSerializer.NormalizeAirportCode(
                request.OriginAirportCode),
            OriginAirportName = NullIfWhiteSpace(
                request.OriginAirportName),
            StartDate = request.StartDate.Date,
            EndDate = request.EndDate.Date,
            CreatedAt = DateTime.UtcNow
        };

        TripRouteSerializer.ReplaceCities(trip, request.Cities);

        await _tripRepository.AddAsync(trip, cancellationToken);
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
