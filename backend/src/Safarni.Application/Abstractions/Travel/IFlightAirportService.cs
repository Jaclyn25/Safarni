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

    Task<IReadOnlyList<FlightAirportSuggestion>>
        SuggestAirportsAsync(
            string country,
            CancellationToken cancellationToken = default);

    Task<FlightRecommendation?> RecommendFlightAsync(
        string originCountry,
        string destinationCity,
        DateTime outboundDate,
        string currencyCode,
        int travelerCount,
        string languageCode,
        CancellationToken cancellationToken = default);
}
