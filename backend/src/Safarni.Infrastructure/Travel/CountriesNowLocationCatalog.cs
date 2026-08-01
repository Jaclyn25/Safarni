using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Safarni.Application.Abstractions.Travel;

namespace Safarni.Infrastructure.Travel;

internal sealed class CountriesNowLocationCatalog
    : ITravelLocationCatalog
{
    private const string CountriesCacheKey =
        "travel-location-countries";
    private static readonly TimeSpan CacheDuration =
        TimeSpan.FromHours(24);
    private static readonly IReadOnlyDictionary<
        string,
        IReadOnlyList<string>>
        TravelDestinationSupplements =
            new Dictionary<
                string,
                IReadOnlyList<string>>(
                StringComparer.OrdinalIgnoreCase)
            {
                ["Turkey"] =
                [
                    "Cappadocia"
                ],
                ["Türkiye"] =
                [
                    "Cappadocia"
                ]
            };

    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CountriesNowLocationCatalog>
        _logger;

    public CountriesNowLocationCatalog(
        HttpClient httpClient,
        IMemoryCache cache,
        ILogger<CountriesNowLocationCatalog> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
    }

    public async Task<IReadOnlyList<TravelCountry>>
        GetCountriesAsync(
            CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(
                CountriesCacheKey,
                out IReadOnlyList<TravelCountry>?
                    cached) &&
            cached is not null)
        {
            return cached;
        }

        try
        {
            using var response = await ExternalHttpResilience.SendAsync(
                _httpClient,
                () => new HttpRequestMessage(
                    HttpMethod.Get,
                    "countries/positions"),
                _logger,
                "CountriesNow",
                "country catalog",
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

            var countries = document.RootElement
                .GetProperty("data")
                .EnumerateArray()
                .Select(country =>
                    new TravelCountry(
                        country.GetProperty("name")
                            .GetString() ?? string.Empty,
                        country.GetProperty("iso2")
                            .GetString() ?? string.Empty))
                .Where(country =>
                    !string.IsNullOrWhiteSpace(
                        country.Name))
                .DistinctBy(
                    country => country.Name,
                    StringComparer.OrdinalIgnoreCase)
                .OrderBy(country => country.Name)
                .ToList();

            _cache.Set(
                CountriesCacheKey,
                countries,
                CacheDuration);

            return countries;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Failed to load the country catalog.");
            return [];
        }
    }

    public async Task<IReadOnlyList<string>>
        GetCitiesAsync(
            string country,
            CancellationToken cancellationToken = default)
    {
        var normalizedCountry = country.Trim();
        var cacheKey =
            $"travel-location-cities:{normalizedCountry.ToLowerInvariant()}";

        if (_cache.TryGetValue(
                cacheKey,
                out IReadOnlyList<string>? cached) &&
            cached is not null)
        {
            return cached;
        }

        try
        {
            using var response =
                await ExternalHttpResilience.SendAsync(
                    _httpClient,
                    () => new HttpRequestMessage(
                        HttpMethod.Post,
                        "countries/cities")
                    {
                        Content = JsonContent.Create(new
                        {
                            country = normalizedCountry
                        })
                    },
                    _logger,
                    "CountriesNow",
                    $"city catalog for {normalizedCountry}",
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

            var cities = document.RootElement
                .GetProperty("data")
                .EnumerateArray()
                .Select(city => city.GetString())
                .Where(city =>
                    !string.IsNullOrWhiteSpace(city))
                .Cast<string>()
                .Concat(
                    GetTravelDestinationSupplements(
                        normalizedCountry))
                .Distinct(
                    StringComparer.OrdinalIgnoreCase)
                .OrderBy(city => city)
                .ToList();

            _cache.Set(
                cacheKey,
                cities,
                CacheDuration);

            return cities;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Failed to load cities for {Country}.",
                normalizedCountry);
            return [];
        }
    }

    private static IReadOnlyList<string>
        GetTravelDestinationSupplements(
            string country)
    {
        return TravelDestinationSupplements.TryGetValue(
            country,
            out var destinations)
                ? destinations
                : [];
    }
}
