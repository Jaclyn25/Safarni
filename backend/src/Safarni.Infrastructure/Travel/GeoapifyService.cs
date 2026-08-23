using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Safarni.Application.Abstractions.Travel;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;
using Safarni.Application.Features.Trips.GetNearbyAttractions;

namespace Safarni.Infrastructure.Travel;

public class GeoapifyService : IGeoapifyService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public GeoapifyService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["GeoapifySettings:ApiKey"] ?? string.Empty;
    }

    public async Task<Result<List<DestinationSuggestion>>> SuggestDestinationsAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        var text = query?.Trim() ?? string.Empty;
        if (text.Length < 2)
            return Result<List<DestinationSuggestion>>.Success([]);
        if (string.IsNullOrWhiteSpace(_apiKey))
            return Result<List<DestinationSuggestion>>.Failure(new Error("Geoapify.Configuration", "Geoapify API key is missing.", ErrorType.Failure));

        try
        {
            var url = $"v1/geocode/autocomplete?text={Uri.EscapeDataString(text)}&type=city&format=json&limit=8&apiKey={_apiKey}";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();
            var payload = await response.Content.ReadFromJsonAsync<GeoapifyAutocompleteResponse>(cancellationToken: cancellationToken);
            var suggestions = (payload?.Results ?? [])
                .Where(result => !string.IsNullOrWhiteSpace(result.City) && !string.IsNullOrWhiteSpace(result.Country))
                .Select(result => new DestinationSuggestion(
                    result.City!.Trim(),
                    result.Country!.Trim(),
                    result.CountryCode?.ToUpperInvariant() ?? string.Empty,
                    string.IsNullOrWhiteSpace(result.Formatted) ? $"{result.City}, {result.Country}" : result.Formatted!,
                    result.Lat,
                    result.Lon))
                .DistinctBy(result => $"{result.City}|{result.Country}", StringComparer.OrdinalIgnoreCase)
                .ToList();
            return Result<List<DestinationSuggestion>>.Success(suggestions);
        }
        catch (Exception exception)
        {
            return Result<List<DestinationSuggestion>>.Failure(new Error("Geoapify.Autocomplete", exception.Message, ErrorType.Failure));
        }
    }

    public async Task<Result<List<NearbyAttractionDto>>> GetNearbyPlacesAsync(
        double latitude,
        double longitude,
        int radiusMeters,
        string? category,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return Result<List<NearbyAttractionDto>>.Failure(new Error(
                "Geoapify.Configuration",
                "Geoapify API key is missing.",
                ErrorType.Failure));
        }

        var limit = 20;
        var categoriesList = string.IsNullOrWhiteSpace(category) 
            ? new[] { "catering.restaurant", "catering.cafe", "accommodation.hotel", "entertainment.museum", "leisure.park", "commercial", "tourism" }
            : new[] { category };

        try
        {
            var allAttractions = new List<NearbyAttractionDto>();

            foreach (var cat in categoriesList)
            {
                var url = $"v2/places?categories={cat}&filter=circle:{longitude},{latitude},{radiusMeters}&bias=proximity:{longitude},{latitude}&limit={limit}&apiKey={_apiKey}";
                var response = await _httpClient.GetAsync(url, cancellationToken);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<GeoapifyPlacesResponse>(
                        cancellationToken: cancellationToken);

                    if (result?.Features != null)
                    {
                        var attractions = result.Features
                            .Where(f => f.Properties != null && !string.IsNullOrWhiteSpace(f.Properties.Name))
                            .Select(f => new NearbyAttractionDto
                            {
                                Name = f.Properties!.Name ?? "Unknown",
                                Category = f.Properties.Categories?.FirstOrDefault(),
                                Address = f.Properties.AddressLine2 ?? f.Properties.Formatted,
                                Latitude = f.Properties.Lat,
                                Longitude = f.Properties.Lon,
                                Distance = f.Properties.Distance,
                                Website = f.Properties.Website,
                                Phone = f.Properties.Contact?.Phone,
                                OpeningHours = f.Properties.OpeningHours
                            })
                            .ToList();
                            
                        allAttractions.AddRange(attractions);
                    }
                }
            }
            
            var distinctAttractions = allAttractions
                .GroupBy(a => new { a.Latitude, a.Longitude, a.Name })
                .Select(g => g.First())
                .OrderBy(a => a.Distance)
                .ToList();

            return Result<List<NearbyAttractionDto>>.Success(distinctAttractions);
        }
        catch (Exception ex)
        {
            return Result<List<NearbyAttractionDto>>.Failure(new Error(
                "Geoapify.Exception",
                ex.Message,
                ErrorType.Failure));
        }
    }

    // Models for deserialization
    private class GeoapifyPlacesResponse
    {
        [JsonPropertyName("features")]
        public List<GeoapifyFeature>? Features { get; set; }
    }

    private class GeoapifyFeature
    {
        [JsonPropertyName("properties")]
        public GeoapifyProperties? Properties { get; set; }
    }

    private class GeoapifyProperties
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("categories")]
        public List<string>? Categories { get; set; }

        [JsonPropertyName("formatted")]
        public string? Formatted { get; set; }

        [JsonPropertyName("address_line2")]
        public string? AddressLine2 { get; set; }

        [JsonPropertyName("lat")]
        public double? Lat { get; set; }

        [JsonPropertyName("lon")]
        public double? Lon { get; set; }

        [JsonPropertyName("distance")]
        public double? Distance { get; set; }

        [JsonPropertyName("website")]
        public string? Website { get; set; }

        [JsonPropertyName("contact")]
        public GeoapifyContact? Contact { get; set; }

        [JsonPropertyName("opening_hours")]
        public string? OpeningHours { get; set; }
    }

    private class GeoapifyContact
    {
        [JsonPropertyName("phone")]
        public string? Phone { get; set; }
    }

    private sealed class GeoapifyAutocompleteResponse
    {
        [JsonPropertyName("results")]
        public List<GeoapifyAutocompleteResult>? Results { get; set; }
    }

    private sealed class GeoapifyAutocompleteResult
    {
        [JsonPropertyName("city")]
        public string? City { get; set; }
        [JsonPropertyName("country")]
        public string? Country { get; set; }
        [JsonPropertyName("country_code")]
        public string? CountryCode { get; set; }
        [JsonPropertyName("formatted")]
        public string? Formatted { get; set; }
        [JsonPropertyName("lat")]
        public double Lat { get; set; }
        [JsonPropertyName("lon")]
        public double Lon { get; set; }
    }
}
