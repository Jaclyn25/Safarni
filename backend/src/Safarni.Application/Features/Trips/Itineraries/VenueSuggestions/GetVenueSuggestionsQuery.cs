using MediatR;
using Safarni.Application.Abstractions.Authentication;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Abstractions.Travel;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;
using Safarni.Application.Features.Trips.GenerateItinerary;
using Safarni.Domain.Entities;

namespace Safarni.Application.Features.Trips.Itineraries.VenueSuggestions;

public sealed record GetVenueSuggestionsQuery(
    Guid TripId,
    Guid ItemId)
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
    private readonly IItineraryTravelDataService _travelData;

    public GetVenueSuggestionsQueryHandler(
        ICurrentUserService currentUser,
        ITripRepository trips,
        IItineraryRepository itineraries,
        IItineraryTravelDataService travelData)
    {
        _currentUser = currentUser;
        _trips = trips;
        _itineraries = itineraries;
        _travelData = travelData;
    }

    public async Task<Result<IReadOnlyList<VenueSuggestionResponse>>>
        Handle(
            GetVenueSuggestionsQuery request,
            CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<IReadOnlyList<VenueSuggestionResponse>>
                .Failure(
                    AuthenticationErrors.AuthenticationRequired);
        }

        var trip = await _trips.GetByIdAsync(
            request.TripId,
            cancellationToken);

        if (trip is null)
        {
            return Result<IReadOnlyList<VenueSuggestionResponse>>
                .Failure(ItineraryErrors.TripNotFound);
        }

        if (trip.UserId != userId)
        {
            return Result<IReadOnlyList<VenueSuggestionResponse>>
                .Failure(ItineraryErrors.Forbidden);
        }

        var itinerary =
            await _itineraries.GetActiveByTripIdAsync(
                request.TripId,
                cancellationToken);
        var day = itinerary?.Days.FirstOrDefault(candidate =>
            candidate.Items.Any(item =>
                item.Id == request.ItemId));
        var item = day?.Items.FirstOrDefault(candidate =>
            candidate.Id == request.ItemId);

        if (itinerary is null || day is null || item is null)
        {
            return Result<IReadOnlyList<VenueSuggestionResponse>>
                .Failure(ItineraryErrors.ItemNotFound);
        }

        var citySchedule =
            TripRouteSerializer.Deserialize(
                trip.SelectedCitiesJson);
        var city = GetCityForDay(
            citySchedule,
            day.DayNumber);

        if (city is null)
        {
            return Result<IReadOnlyList<VenueSuggestionResponse>>
                .Failure(ItineraryErrors.InvalidAiResponse);
        }

        IReadOnlyList<
            ChatBotApi.Application.Common.Models.GooglePlaces.PlaceDetails>
            places;

        if (item.Category ==
            ItineraryItemCategory.Accommodation)
        {
            var preferences =
                GenerateItineraryPreferencesSerializer
                    .Deserialize(itinerary.Preferences);
            var startDay = GetStartDay(
                citySchedule,
                city.Name);
            var checkIn = trip.StartDate.Date
                .AddDays(startDay - 1);
            var checkOut = checkIn.AddDays(city.Days);

            places = await _travelData.SearchHotelsAsync(
                city.Name,
                preferences.AccommodationStyle,
                checkIn,
                checkOut,
                itinerary.TravelerCount,
                itinerary.CurrencyCode,
                preferences.PreferredLanguage,
                cancellationToken);
        }
        else if (item.Category ==
                 ItineraryItemCategory.Meal)
        {
            var preferences =
                GenerateItineraryPreferencesSerializer
                    .Deserialize(itinerary.Preferences);
            var mealPeriod = GetMealPeriod(item);

            places = await _travelData.SearchLocalAsync(
                $"popular {mealPeriod} restaurants and cafes in {city.Name}",
                city.Name,
                preferences.PreferredLanguage,
                cancellationToken);
        }
        else
        {
            return Result<IReadOnlyList<VenueSuggestionResponse>>
                .Failure(
                    new Error(
                        "Itinerary.UnsupportedVenueSelection",
                        "Venue choices are available only for meals and accommodation.",
                        ErrorType.Validation));
        }

        var eligiblePlaces = places
            .Where(place =>
                !string.IsNullOrWhiteSpace(place.Name))
            .Where(place =>
                !string.Equals(
                    place.PlaceId,
                    item.GooglePlaceId,
                    StringComparison.OrdinalIgnoreCase))
            .ToList();
        var cityMatchedPlaces = eligiblePlaces
            .Where(place =>
                place.Address.Contains(
                    city.Name,
                    StringComparison.OrdinalIgnoreCase))
            .ToList();
        var suggestions = (cityMatchedPlaces.Count > 0
                ? cityMatchedPlaces
                : eligiblePlaces)
            .Take(6)
            .Select(place =>
                new VenueSuggestionResponse(
                    place.PlaceId,
                    place.Name,
                    place.Description,
                    place.Address,
                    place.GoogleMapsLink,
                    place.Rating,
                    place.PhotoUrl,
                    place.PriceLevel,
                    place.RatePerNight,
                    place.TotalRate,
                    place.PriceSource,
                    place.ExternalUrl,
                    place.Latitude,
                    place.Longitude))
            .ToList();

        return Result<IReadOnlyList<VenueSuggestionResponse>>
            .Success(suggestions);
    }

    private static TripCitySelection? GetCityForDay(
        IReadOnlyList<TripCitySelection> cities,
        int dayNumber)
    {
        var cursor = 1;

        foreach (var city in cities)
        {
            if (dayNumber >= cursor &&
                dayNumber < cursor + city.Days)
            {
                return city;
            }

            cursor += city.Days;
        }

        return null;
    }

    private static int GetStartDay(
        IReadOnlyList<TripCitySelection> cities,
        string cityName)
    {
        var startDay = 1;

        foreach (var city in cities)
        {
            if (string.Equals(
                    city.Name,
                    cityName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return startDay;
            }

            startDay += city.Days;
        }

        return 1;
    }

    private static string GetMealPeriod(
        ItineraryItem item)
    {
        if (item.Title.Contains(
                "breakfast",
                StringComparison.OrdinalIgnoreCase) ||
            item.StartTime < TimeSpan.FromHours(11))
        {
            return "breakfast";
        }

        if (item.Title.Contains(
                "dinner",
                StringComparison.OrdinalIgnoreCase) ||
            item.StartTime >= TimeSpan.FromHours(17))
        {
            return "dinner";
        }

        return "lunch";
    }
}
