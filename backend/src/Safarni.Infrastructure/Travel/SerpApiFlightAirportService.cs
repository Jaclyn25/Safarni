using System.Text.Json;
using System.Globalization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Safarni.Application.Abstractions.Travel;

namespace Safarni.Infrastructure.Travel;

internal sealed class SerpApiFlightAirportService
    : IFlightAirportService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SerpApiFlightAirportService>
        _logger;
    private readonly string _apiKey;

    public SerpApiFlightAirportService(
        HttpClient httpClient,
        IConfiguration configuration,
        IMemoryCache cache,
        ILogger<SerpApiFlightAirportService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
        _apiKey =
            configuration["SerpApi:ApiKey"]
            ?? string.Empty;
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_apiKey);

    public async Task<IReadOnlyList<
        FlightAirportSuggestion>> SuggestAirportsAsync(
        string country,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return [];
        }

        var normalizedCountry = country.Trim();
        var cacheKey =
            $"flight-airports:{normalizedCountry.ToLowerInvariant()}";

        if (_cache.TryGetValue(
                cacheKey,
                out IReadOnlyList<FlightAirportSuggestion>?
                    cached) &&
            cached is not null)
        {
            return cached;
        }

        var uri =
            "https://serpapi.com/search.json" +
            "?engine=google_flights_autocomplete" +
            "&q=" +
            Uri.EscapeDataString(normalizedCountry) +
            "&hl=en&api_key=" +
            Uri.EscapeDataString(_apiKey);

        try
        {
            using var response =
                await _httpClient.GetAsync(
                    uri,
                    cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream =
                await response.Content.ReadAsStreamAsync(
                    cancellationToken);
            using var document =
                await JsonDocument.ParseAsync(
                    stream,
                    cancellationToken:
                        cancellationToken);

            if (!document.RootElement.TryGetProperty(
                    "suggestions",
                    out var suggestions) ||
                suggestions.ValueKind !=
                    JsonValueKind.Array)
            {
                return [];
            }

            var airports = suggestions
                .EnumerateArray()
                .Where(suggestion =>
                    suggestion.TryGetProperty(
                        "airports",
                        out var values) &&
                    values.ValueKind ==
                        JsonValueKind.Array)
                .SelectMany(suggestion =>
                    suggestion.GetProperty("airports")
                        .EnumerateArray())
                .Select(airport =>
                    new FlightAirportSuggestion(
                        GetString(airport, "id"),
                        GetString(airport, "name"),
                        GetString(airport, "city")))
                .Where(airport =>
                    airport.Code.Length == 3 &&
                    airport.Code.All(
                        char.IsLetter) &&
                    !string.IsNullOrWhiteSpace(
                        airport.Name))
                .DistinctBy(
                    airport => airport.Code,
                    StringComparer.OrdinalIgnoreCase)
                .Take(10)
                .ToList();

            _cache.Set(
                cacheKey,
                airports,
                TimeSpan.FromHours(24));

            return airports;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Failed to suggest airports for {Country}.",
                normalizedCountry);
            return [];
        }
    }

    public async Task<FlightRecommendation?>
        RecommendFlightAsync(
            string originCountry,
            string destinationCity,
            DateTime outboundDate,
            string currencyCode,
            int travelerCount,
            string languageCode,
            CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return null;
        }

        var originAirports =
            await SuggestAirportsAsync(
                originCountry,
                cancellationToken);
        var destinationAirports =
            await SuggestAirportsAsync(
                destinationCity,
                cancellationToken);

        var departureIds = string.Join(
            ',',
            originAirports.Take(3)
                .Select(airport => airport.Code));
        var arrivalIds = string.Join(
            ',',
            destinationAirports.Take(3)
                .Select(airport => airport.Code));

        if (string.IsNullOrWhiteSpace(departureIds) ||
            string.IsNullOrWhiteSpace(arrivalIds))
        {
            return null;
        }

        var normalizedCurrency =
            currencyCode.Trim().ToUpperInvariant();
        var cacheKey = string.Join(
            ':',
            "flight-recommendation",
            departureIds,
            arrivalIds,
            outboundDate.ToString("yyyy-MM-dd"),
            normalizedCurrency,
            travelerCount);

        if (_cache.TryGetValue(
                cacheKey,
                out FlightRecommendation? cached) &&
            cached is not null)
        {
            return cached;
        }

        var uri =
            "https://serpapi.com/search.json" +
            "?engine=google_flights" +
            "&type=2" +
            "&departure_id=" +
            Uri.EscapeDataString(departureIds) +
            "&arrival_id=" +
            Uri.EscapeDataString(arrivalIds) +
            "&outbound_date=" +
            outboundDate.ToString(
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture) +
            "&currency=" +
            Uri.EscapeDataString(normalizedCurrency) +
            "&adults=" +
            Math.Clamp(travelerCount, 1, 9)
                .ToString(CultureInfo.InvariantCulture) +
            "&travel_class=1&sort_by=1&hl=" +
            (string.Equals(
                languageCode,
                "ar",
                StringComparison.OrdinalIgnoreCase)
                    ? "ar"
                    : "en") +
            "&api_key=" +
            Uri.EscapeDataString(_apiKey);

        try
        {
            using var response =
                await _httpClient.GetAsync(
                    uri,
                    cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Google Flights search returned HTTP {StatusCode} for {OriginCountry} to {DestinationCity}.",
                    response.StatusCode,
                    originCountry,
                    destinationCity);
                return null;
            }

            await using var stream =
                await response.Content.ReadAsStreamAsync(
                    cancellationToken);
            using var document =
                await JsonDocument.ParseAsync(
                    stream,
                    cancellationToken:
                        cancellationToken);

            var root = document.RootElement;
            var flight = GetFirstFlightResult(root);

            if (flight is null ||
                !flight.Value.TryGetProperty(
                    "flights",
                    out var legs) ||
                legs.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            var legList = legs.EnumerateArray().ToList();

            if (legList.Count == 0)
            {
                return null;
            }

            var firstLeg = legList[0];
            var lastLeg = legList[^1];
            var departureAirport =
                GetObject(firstLeg, "departure_airport");
            var arrivalAirport =
                GetObject(lastLeg, "arrival_airport");
            var price = GetDecimal(
                flight.Value,
                "price");

            if (departureAirport is null ||
                arrivalAirport is null ||
                price is null)
            {
                return null;
            }

            var recommendation =
                new FlightRecommendation(
                    GetString(
                        departureAirport.Value,
                        "id"),
                    GetString(
                        departureAirport.Value,
                        "name"),
                    GetString(
                        arrivalAirport.Value,
                        "id"),
                    GetString(
                        arrivalAirport.Value,
                        "name"),
                    GetString(
                        departureAirport.Value,
                        "time"),
                    GetString(
                        arrivalAirport.Value,
                        "time"),
                    string.Join(
                        ", ",
                        legList
                            .Select(leg =>
                                GetString(
                                    leg,
                                    "airline"))
                            .Where(value =>
                                !string.IsNullOrWhiteSpace(
                                    value))
                            .Distinct(
                                StringComparer.OrdinalIgnoreCase)),
                    string.Join(
                        ", ",
                        legList
                            .Select(leg =>
                                GetString(
                                    leg,
                                    "flight_number"))
                            .Where(value =>
                                !string.IsNullOrWhiteSpace(
                                    value))),
                    GetInt(
                        flight.Value,
                        "total_duration") ?? 0,
                    Math.Max(0, legList.Count - 1),
                    price.Value,
                    normalizedCurrency,
                    GetString(
                        flight.Value,
                        "airline_logo"),
                    GetNestedString(
                        root,
                        "search_metadata",
                        "google_flights_url"));

            _cache.Set(
                cacheKey,
                recommendation,
                TimeSpan.FromMinutes(30));

            return recommendation;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Failed to recommend a flight from {OriginCountry} to {DestinationCity}.",
                originCountry,
                destinationCity);
            return null;
        }
    }

    private static JsonElement? GetFirstFlightResult(
        JsonElement root)
    {
        foreach (var propertyName in new[]
                 {
                     "best_flights",
                     "other_flights"
                 })
        {
            if (root.TryGetProperty(
                    propertyName,
                    out var flights) &&
                flights.ValueKind == JsonValueKind.Array)
            {
                var first =
                    flights.EnumerateArray()
                        .FirstOrDefault();

                if (first.ValueKind == JsonValueKind.Object)
                {
                    return first;
                }
            }
        }

        return null;
    }

    private static JsonElement? GetObject(
        JsonElement element,
        string propertyName)
    {
        return element.TryGetProperty(
                   propertyName,
                   out var value) &&
               value.ValueKind == JsonValueKind.Object
            ? value
            : null;
    }

    private static string GetNestedString(
        JsonElement element,
        string objectName,
        string propertyName)
    {
        var nested = GetObject(
            element,
            objectName);

        return nested is null
            ? string.Empty
            : GetString(
                nested.Value,
                propertyName);
    }

    private static int? GetInt(
        JsonElement element,
        string propertyName)
    {
        if (!element.TryGetProperty(
                propertyName,
                out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number &&
            value.TryGetInt32(out var number))
        {
            return number;
        }

        return value.ValueKind == JsonValueKind.String &&
               int.TryParse(
                   value.GetString(),
                   NumberStyles.Integer,
                   CultureInfo.InvariantCulture,
                   out var parsed)
            ? parsed
            : null;
    }

    private static decimal? GetDecimal(
        JsonElement element,
        string propertyName)
    {
        if (!element.TryGetProperty(
                propertyName,
                out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number &&
            value.TryGetDecimal(out var number))
        {
            return number;
        }

        return value.ValueKind == JsonValueKind.String &&
               decimal.TryParse(
                   value.GetString(),
                   NumberStyles.Any,
                   CultureInfo.InvariantCulture,
                   out var parsed)
            ? parsed
            : null;
    }

    private static string GetString(
        JsonElement element,
        string propertyName)
    {
        return element.TryGetProperty(
                   propertyName,
                   out var value) &&
               value.ValueKind ==
                   JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;
    }
}
