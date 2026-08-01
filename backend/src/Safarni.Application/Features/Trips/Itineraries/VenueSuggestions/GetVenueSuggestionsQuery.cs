using MediatR;
using Safarni.Application.Abstractions.Authentication;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;
using Safarni.Application.Features.Trips.GenerateItinerary;
using Safarni.Domain.Entities;

namespace Safarni.Application.Features.Trips.Itineraries.VenueSuggestions;

public sealed record GetVenueSuggestionsQuery(
    Guid TripId,
    Guid ItemId)
    : IRequest<Result<IReadOnlyList<VenueSuggestionResponse>>>;

public sealed record GetDayVenueSuggestionsQuery(
    Guid TripId,
    Guid DayId,
    string Category)
    : IRequest<Result<IReadOnlyList<VenueSuggestionResponse>>>;

public sealed record VenueSuggestionResponse(
    string PlaceId,
    string Name,
    string? Description,
    string? Address,
    string? GoogleMapsUrl,
    double? Rating,
    string? PhotoUrl,
    string? PriceLevel,
    decimal? RatePerNight,
    decimal? TotalRate,
    string? PriceSource,
    string? ExternalUrl,
    double? Latitude,
    double? Longitude);

public sealed class GetVenueSuggestionsQueryHandler
    : IRequestHandler<
        GetVenueSuggestionsQuery,
        Result<IReadOnlyList<VenueSuggestionResponse>>>
{
    private readonly ICurrentUserService _currentUser;
    private readonly ITripRepository _trips;
    private readonly IItineraryRepository _itineraries;
    private readonly ItineraryVenueSuggestionLoader _loader;

    public GetVenueSuggestionsQueryHandler(
        ICurrentUserService currentUser,
        ITripRepository trips,
        IItineraryRepository itineraries,
        ItineraryVenueSuggestionLoader loader)
    {
        _currentUser = currentUser;
        _trips = trips;
        _itineraries = itineraries;
        _loader = loader;
    }

    public async Task<Result<IReadOnlyList<VenueSuggestionResponse>>> Handle(
        GetVenueSuggestionsQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<IReadOnlyList<VenueSuggestionResponse>>.Failure(
                AuthenticationErrors.AuthenticationRequired);
        }

        var trip = await _trips.GetByIdAsync(
            request.TripId,
            cancellationToken);

        if (trip is null)
        {
            return Result<IReadOnlyList<VenueSuggestionResponse>>.Failure(
                ItineraryErrors.TripNotFound);
        }

        if (trip.UserId != userId)
        {
            return Result<IReadOnlyList<VenueSuggestionResponse>>.Failure(
                ItineraryErrors.Forbidden);
        }

        var itinerary = await _itineraries.GetActiveByTripIdAsync(
            request.TripId,
            cancellationToken);
        var day = itinerary?.Days.FirstOrDefault(candidate =>
            candidate.Items.Any(item => item.Id == request.ItemId));
        var item = day?.Items.FirstOrDefault(candidate =>
            candidate.Id == request.ItemId);

        if (itinerary is null || day is null || item is null)
        {
            return Result<IReadOnlyList<VenueSuggestionResponse>>.Failure(
                ItineraryErrors.ItemNotFound);
        }

        if (item.FlightDepartureAirportCode is not null)
        {
            return Result<IReadOnlyList<VenueSuggestionResponse>>.Failure(
                ItineraryErrors.UnsupportedVenueSelection);
        }

        IReadOnlyList<LoadedVenueSuggestion> loaded;
        try
        {
            loaded = await _loader.LoadAsync(
                trip,
                itinerary,
                day,
                item,
                cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return Result<IReadOnlyList<VenueSuggestionResponse>>.Failure(
                TripErrors.CorruptRoute);
        }

        var suggestions = VenueSuggestionResponseMapper.Map(loaded);

        return Result<IReadOnlyList<VenueSuggestionResponse>>.Success(
            suggestions);
    }
}

public sealed class GetDayVenueSuggestionsQueryHandler
    : IRequestHandler<
        GetDayVenueSuggestionsQuery,
        Result<IReadOnlyList<VenueSuggestionResponse>>>
{
    private readonly ICurrentUserService _currentUser;
    private readonly ITripRepository _trips;
    private readonly IItineraryRepository _itineraries;
    private readonly ItineraryVenueSuggestionLoader _loader;

    public GetDayVenueSuggestionsQueryHandler(
        ICurrentUserService currentUser,
        ITripRepository trips,
        IItineraryRepository itineraries,
        ItineraryVenueSuggestionLoader loader)
    {
        _currentUser = currentUser;
        _trips = trips;
        _itineraries = itineraries;
        _loader = loader;
    }

    public async Task<Result<IReadOnlyList<VenueSuggestionResponse>>> Handle(
        GetDayVenueSuggestionsQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<IReadOnlyList<VenueSuggestionResponse>>.Failure(
                AuthenticationErrors.AuthenticationRequired);
        }

        var trip = await _trips.GetByIdAsync(
            request.TripId,
            cancellationToken);

        if (trip is null)
        {
            return Result<IReadOnlyList<VenueSuggestionResponse>>.Failure(
                ItineraryErrors.TripNotFound);
        }

        if (trip.UserId != userId)
        {
            return Result<IReadOnlyList<VenueSuggestionResponse>>.Failure(
                ItineraryErrors.Forbidden);
        }

        var itinerary = await _itineraries.GetActiveByTripIdAsync(
            request.TripId,
            cancellationToken);
        var day = itinerary?.Days.FirstOrDefault(candidate =>
            candidate.Id == request.DayId);

        if (itinerary is null || day is null)
        {
            return Result<IReadOnlyList<VenueSuggestionResponse>>.Failure(
                ItineraryErrors.DayNotFound);
        }

        if (!ItineraryItemInput.IsValidCategory(request.Category))
        {
            return Result<IReadOnlyList<VenueSuggestionResponse>>.Failure(
                ItineraryErrors.UnsupportedVenueSelection);
        }

        IReadOnlyList<LoadedVenueSuggestion> loaded;
        try
        {
            loaded = await _loader.LoadAsync(
                trip,
                itinerary,
                day,
                ItineraryItemInput.ParseCategory(request.Category),
                null,
                cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return Result<IReadOnlyList<VenueSuggestionResponse>>.Failure(
                TripErrors.CorruptRoute);
        }

        return Result<IReadOnlyList<VenueSuggestionResponse>>.Success(
            VenueSuggestionResponseMapper.Map(loaded));
    }
}

internal static class VenueSuggestionResponseMapper
{
    public static IReadOnlyList<VenueSuggestionResponse> Map(
        IReadOnlyList<LoadedVenueSuggestion> loaded)
    {
        return loaded
            .Take(8)
            .Select(candidate => new VenueSuggestionResponse(
                candidate.Place.PlaceId,
                candidate.Place.Name,
                candidate.Place.Description,
                candidate.Place.Address,
                candidate.Place.GoogleMapsLink,
                candidate.Place.Rating,
                candidate.Place.PhotoUrl,
                candidate.Place.PriceLevel,
                candidate.Place.RatePerNight,
                candidate.Place.TotalRate,
                candidate.Place.PriceSource,
                candidate.Place.ExternalUrl,
                candidate.Place.Latitude,
                candidate.Place.Longitude))
            .ToList();
    }
}
