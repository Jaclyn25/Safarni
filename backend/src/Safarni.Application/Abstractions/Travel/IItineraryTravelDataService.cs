using ChatBotApi.Application.Common.Models.GooglePlaces;

namespace Safarni.Application.Abstractions.Travel;

public interface IItineraryTravelDataService
{
    bool IsConfigured { get; }

    Task<IReadOnlyList<PlaceDetails>> SearchLocalAsync(
        string query,
        string destination,
        string languageCode,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PlaceDetails>> SearchHotelsAsync(
        string destination,
        string accommodationStyle,
        DateTime checkInDate,
        DateTime checkOutDate,
        int travelerCount,
        string currencyCode,
        string languageCode,
        CancellationToken cancellationToken = default);
}

public interface IItineraryRouteService
{
    bool IsConfigured { get; }

    Task<ItineraryRouteResult?> ComputeRouteAsync(
        ItineraryRoutePoint origin,
        ItineraryRoutePoint destination,
        IReadOnlyList<string> transportationPreferences,
        string languageCode,
        CancellationToken cancellationToken = default);
}

public sealed record ItineraryRoutePoint(
    double Latitude,
    double Longitude);

public sealed record ItineraryRouteResult(
    int DistanceMeters,
    int DurationMinutes,
    string TravelMode,
    string? TransitDetails);
