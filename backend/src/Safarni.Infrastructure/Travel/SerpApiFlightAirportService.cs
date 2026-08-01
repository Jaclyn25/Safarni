using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Safarni.Application.Abstractions.Travel;

namespace Safarni.Infrastructure.Travel;

internal sealed class SerpApiFlightAirportService
    : IFlightAirportService
{
    private static readonly SemaphoreSlim RequestGate = new(2, 2);

    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ITravelLocationCatalog _locationCatalog;
    private readonly ILogger<SerpApiFlightAirportService> _logger;
    private readonly string _apiKey;

    public SerpApiFlightAirportService(
        HttpClient httpClient,
        IConfiguration configuration,
        IMemoryCache cache,
        ITravelLocationCatalog locationCatalog,
        ILogger<SerpApiFlightAirportService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _locationCatalog = locationCatalog;
        _logger = logger;
        _apiKey = configuration["SerpApi:ApiKey"] ?? string.Empty;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

    public async Task<IReadOnlyList<FlightAirportSuggestion>>
        SuggestAirportsAsync(
            string query,
            CancellationToken cancellationToken = default)
    {
        if (!IsConfigured || string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        var normalizedCountry = query.Trim();
        var cacheKey =
            $"flight-airports:v2:{normalizedCountry.ToLowerInvariant()}";

        if (_cache.TryGetValue(
                cacheKey,
                out IReadOnlyList<FlightAirportSuggestion>? cached) &&
            cached is not null)
        {
            return cached;
        }

        // The autocomplete endpoint can return several suggestions for one
        // query. We must not flatten every airport blindly, otherwise airports
        // from unrelated suggestions can leak into the selected country.
        var knownCities = await _locationCatalog.GetCitiesAsync(
            normalizedCountry,
            cancellationToken);
        var countries = await _locationCatalog.GetCountriesAsync(
            cancellationToken);
        var selectedCountry = countries.FirstOrDefault(country =>
            string.Equals(
                country.Name,
                normalizedCountry,
                StringComparison.OrdinalIgnoreCase));

        var knownCityKeys = knownCities
            .Select(NormalizeForMatch)
            .Where(value => value.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var countryAliases = BuildCountryAliases(
            normalizedCountry,
            selectedCountry?.Iso2);

        var uri =
            "https://serpapi.com/search.json" +
            "?engine=google_flights_autocomplete" +
            "&q=" + Uri.EscapeDataString(normalizedCountry) +
            "&hl=en" +
            "&exclude_regions=true" +
            (string.IsNullOrWhiteSpace(selectedCountry?.Iso2)
                ? string.Empty
                : "&gl=" + Uri.EscapeDataString(
                    selectedCountry.Iso2.ToLowerInvariant())) +
            "&api_key=" + Uri.EscapeDataString(_apiKey);

        await RequestGate.WaitAsync(cancellationToken);
        try
        {
            using var response = await ExternalHttpResilience.SendAsync(
                _httpClient,
                () => new HttpRequestMessage(HttpMethod.Get, uri),
                _logger,
                "SerpApi",
                "airport autocomplete",
                cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream =
                await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(
                stream,
                cancellationToken: cancellationToken);

            if (!document.RootElement.TryGetProperty(
                    "suggestions",
                    out var suggestions) ||
                suggestions.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var rawAirportCount = 0;
            var filtered = new List<FlightAirportSuggestion>();

            foreach (var suggestion in suggestions.EnumerateArray())
            {
                if (!suggestion.TryGetProperty(
                        "airports",
                        out var airportValues) ||
                    airportValues.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                var suggestionName = GetString(suggestion, "name");
                var suggestionDescription =
                    GetString(suggestion, "description");
                var suggestionMatchesCountry =
                    TextMentionsCountry(
                        $"{suggestionName} {suggestionDescription}",
                        countryAliases);

                foreach (var airport in airportValues.EnumerateArray())
                {
                    rawAirportCount++;

                    var code =
                        NormalizeIata(GetString(airport, "id"))
                        ?? string.Empty;
                    var name = GetString(airport, "name");
                    var city = GetString(airport, "city");

                    if (!IsIata(code) ||
                        string.IsNullOrWhiteSpace(name))
                    {
                        continue;
                    }

                    var cityMatchesCountry =
                        knownCityKeys.Contains(NormalizeForMatch(city));

                    if (!cityMatchesCountry &&
                        !suggestionMatchesCountry)
                    {
                        continue;
                    }

                    filtered.Add(
                        new FlightAirportSuggestion(
                            code,
                            name,
                            city));
                }
            }

            var airports = filtered
                .DistinctBy(
                    airport => airport.Code,
                    StringComparer.OrdinalIgnoreCase)
                .Take(15)
                .ToList();

            _logger.LogInformation(
                "Airport autocomplete for {Country}: {RawCount} raw airports, {FilteredCount} matched the selected country.",
                normalizedCountry,
                rawAirportCount,
                airports.Count);

            // Returning an empty list is safer than showing airports from a
            // different country when the upstream result cannot be verified.
            _cache.Set(cacheKey, airports, TimeSpan.FromHours(24));
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
        finally
        {
            RequestGate.Release();
        }
    }

    public async Task<FlightRecommendation?> RecommendFlightAsync(
        string departureAirportCode,
        string arrivalAirportCode,
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

        var departureCode = NormalizeIata(departureAirportCode);
        var arrivalCode = NormalizeIata(arrivalAirportCode);

        if (departureCode is null ||
            arrivalCode is null ||
            string.Equals(
                departureCode,
                arrivalCode,
                StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var normalizedCurrency = currencyCode.Trim().ToUpperInvariant();
        var cacheKey = string.Join(
            ':',
            "flight-recommendation",
            departureCode,
            arrivalCode,
            outboundDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
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
            "&departure_id=" + Uri.EscapeDataString(departureCode) +
            "&arrival_id=" + Uri.EscapeDataString(arrivalCode) +
            "&outbound_date=" + outboundDate.ToString(
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture) +
            "&currency=" + Uri.EscapeDataString(normalizedCurrency) +
            "&adults=" + Math.Clamp(travelerCount, 1, 9)
                .ToString(CultureInfo.InvariantCulture) +
            "&travel_class=1&sort_by=1&hl=" +
            (string.Equals(
                languageCode,
                "ar",
                StringComparison.OrdinalIgnoreCase)
                    ? "ar"
                    : "en") +
            "&api_key=" + Uri.EscapeDataString(_apiKey);

        await RequestGate.WaitAsync(cancellationToken);
        try
        {
            using var response = await ExternalHttpResilience.SendAsync(
                _httpClient,
                () => new HttpRequestMessage(HttpMethod.Get, uri),
                _logger,
                "SerpApi",
                $"flight search {departureCode}-{arrivalCode}",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Google Flights returned HTTP {StatusCode} for exact route {DepartureCode}-{ArrivalCode}.",
                    response.StatusCode,
                    departureCode,
                    arrivalCode);
                return null;
            }

            await using var stream =
                await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(
                stream,
                cancellationToken: cancellationToken);

            var root = document.RootElement;
            var flight = GetMatchingFlightResult(
                root,
                departureCode,
                arrivalCode);

            if (flight is null ||
                !flight.Value.TryGetProperty("flights", out var legs) ||
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
            var departureAirport = GetObject(firstLeg, "departure_airport");
            var arrivalAirport = GetObject(lastLeg, "arrival_airport");
            var price = GetDecimal(flight.Value, "price");

            if (departureAirport is null ||
                arrivalAirport is null ||
                price is null)
            {
                return null;
            }

            var returnedDepartureCode = NormalizeIata(
                GetString(departureAirport.Value, "id"));
            var returnedArrivalCode = NormalizeIata(
                GetString(arrivalAirport.Value, "id"));

            // Critical correctness guard. Never accept an unrelated airport.
            if (!string.Equals(
                    returnedDepartureCode,
                    departureCode,
                    StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(
                    returnedArrivalCode,
                    arrivalCode,
                    StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "Rejected Google Flights result {ReturnedDeparture}-{ReturnedArrival}; expected {Departure}-{Arrival}.",
                    returnedDepartureCode,
                    returnedArrivalCode,
                    departureCode,
                    arrivalCode);
                return null;
            }

            var recommendation = new FlightRecommendation(
                departureCode,
                GetString(departureAirport.Value, "name"),
                arrivalCode,
                GetString(arrivalAirport.Value, "name"),
                GetString(departureAirport.Value, "time"),
                GetString(arrivalAirport.Value, "time"),
                string.Join(
                    ", ",
                    legList
                        .Select(leg => GetString(leg, "airline"))
                        .Where(value => !string.IsNullOrWhiteSpace(value))
                        .Distinct(StringComparer.OrdinalIgnoreCase)),
                string.Join(
                    ", ",
                    legList
                        .Select(leg => GetString(leg, "flight_number"))
                        .Where(value => !string.IsNullOrWhiteSpace(value))),
                GetInt(flight.Value, "total_duration") ?? 0,
                Math.Max(0, legList.Count - 1),
                price.Value,
                normalizedCurrency,
                GetString(flight.Value, "airline_logo"),
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
                "Failed to recommend a flight for exact route {DepartureCode}-{ArrivalCode}.",
                departureCode,
                arrivalCode);
            return null;
        }
        finally
        {
            RequestGate.Release();
        }
    }

    private static IReadOnlySet<string> BuildCountryAliases(
        string countryName,
        string? iso2)
    {
        var aliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            NormalizeForMatch(countryName)
        };

        if (!string.IsNullOrWhiteSpace(iso2))
        {
            try
            {
                var region = new RegionInfo(iso2);
                aliases.Add(NormalizeForMatch(region.EnglishName));
                aliases.Add(NormalizeForMatch(region.NativeName));
                aliases.Add(NormalizeForMatch(region.DisplayName));
            }
            catch (ArgumentException)
            {
                // The city catalog still provides the primary country check.
            }
        }

        aliases.RemoveWhere(string.IsNullOrWhiteSpace);
        return aliases;
    }

    private static bool TextMentionsCountry(
        string text,
        IReadOnlySet<string> countryAliases)
    {
        var normalizedText = NormalizeForMatch(text);

        return countryAliases.Any(alias =>
            alias.Length > 0 &&
            normalizedText.Contains(
                alias,
                StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeForMatch(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var decomposed = value
            .Trim()
            .Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) ==
                UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
            }
            else
            {
                builder.Append(' ');
            }
        }

        return string.Join(
            ' ',
            builder
                .ToString()
                .Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries));
    }

    private static JsonElement? GetMatchingFlightResult(
        JsonElement root,
        string departureCode,
        string arrivalCode)
    {
        foreach (var propertyName in new[] { "best_flights", "other_flights" })
        {
            if (!root.TryGetProperty(propertyName, out var flights) ||
                flights.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var candidate in flights.EnumerateArray())
            {
                if (!candidate.TryGetProperty("flights", out var legs) ||
                    legs.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                var legList = legs.EnumerateArray().ToList();
                if (legList.Count == 0)
                {
                    continue;
                }

                var firstAirport = GetObject(
                    legList[0],
                    "departure_airport");
                var lastAirport = GetObject(
                    legList[^1],
                    "arrival_airport");

                if (firstAirport is null || lastAirport is null)
                {
                    continue;
                }

                if (string.Equals(
                        NormalizeIata(GetString(firstAirport.Value, "id")),
                        departureCode,
                        StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(
                        NormalizeIata(GetString(lastAirport.Value, "id")),
                        arrivalCode,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return candidate;
                }
            }
        }

        return null;
    }

    private static bool IsIata(string? value) =>
        value is { Length: 3 } && value.All(char.IsLetter);

    private static string? NormalizeIata(string? value)
    {
        var normalized = string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim().ToUpperInvariant();
        return IsIata(normalized) ? normalized : null;
    }

    private static JsonElement? GetObject(
        JsonElement element,
        string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) &&
               value.ValueKind == JsonValueKind.Object
            ? value
            : null;
    }

    private static string GetNestedString(
        JsonElement element,
        string objectName,
        string propertyName)
    {
        var nested = GetObject(element, objectName);
        return nested is null
            ? string.Empty
            : GetString(nested.Value, propertyName);
    }

    private static int? GetInt(
        JsonElement element,
        string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
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
        if (!element.TryGetProperty(propertyName, out var value))
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
        return element.TryGetProperty(propertyName, out var value) &&
               value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;
    }
}