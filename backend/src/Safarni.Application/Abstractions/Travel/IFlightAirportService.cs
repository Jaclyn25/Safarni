namespace Safarni.Application.Abstractions.Travel;

public sealed record FlightAirportSuggestion(
    string Code,
    string Name,
    string City);

public sealed record FlightRecommendation(
    string DepartureAirportCode,
    string DepartureAirportName,
    string ArrivalAirportCode,
    string ArrivalAirportName,
    string DepartureTime,
    string ArrivalTime,
    string Airlines,
    string FlightNumbers,
    int TotalDurationMinutes,
    int StopCount,
    decimal Price,
    string CurrencyCode,
    string? AirlineLogoUrl,
    string GoogleFlightsUrl);

public interface IFlightAirportService
{
    bool IsConfigured { get; }

    Task<IReadOnlyList<FlightAirportSuggestion>> SuggestAirportsAsync(
        string query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches an exact IATA-to-IATA route. The implementation must reject
    /// any result whose first departure or final arrival code differs from the
    /// requested codes.
    /// </summary>
    Task<FlightRecommendation?> RecommendFlightAsync(
        string departureAirportCode,
        string arrivalAirportCode,
        DateTime outboundDate,
        string currencyCode,
        int travelerCount,
        string languageCode,
        CancellationToken cancellationToken = default);
}
