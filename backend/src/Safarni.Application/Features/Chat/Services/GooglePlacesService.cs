using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ChatBotApi.Application.Common.Interfaces;
using ChatBotApi.Application.Common.Models.GooglePlaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ChatBotApi.Application.Features.Chat.Services;

public class GooglePlacesService : IGooglePlacesService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _baseUrl;
    private readonly ILogger<GooglePlacesService> _logger;

    public GooglePlacesService(
        HttpClient httpClient, 
        IConfiguration configuration, 
        ILogger<GooglePlacesService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = configuration["GooglePlaces:ApiKey"] ?? string.Empty;
        _baseUrl = configuration["GooglePlaces:BaseUrl"] ?? "https://maps.googleapis.com/maps/api/place";

        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogWarning("GooglePlaces:ApiKey is not configured in appsettings.json.");
        }
    }

    public async Task<List<PlaceDetails>> TextSearchAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogWarning("Google Places API Key is missing. Returning empty results.");
            return new List<PlaceDetails>();
        }

        // 1. Try Places API (New) v1 searchText endpoint first
        try
        {
            var v1Url = "https://places.googleapis.com/v1/places:searchText";
            _logger.LogInformation("Calling Google Places API (New) v1 searchText for query: {Query}", query);

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, v1Url);
            requestMessage.Headers.Add("X-Goog-Api-Key", _apiKey);
            requestMessage.Headers.Add("X-Goog-FieldMask", "places.id,places.displayName,places.formattedAddress,places.rating,places.userRatingCount,places.googleMapsUri,places.photos,places.editorialSummary,places.types");

            var payload = new { textQuery = query, languageCode = "ar" };
            var jsonPayload = JsonSerializer.Serialize(payload);
            requestMessage.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var searchResponse = JsonSerializer.Deserialize<PlacesNewSearchResponse>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (searchResponse?.Places != null && searchResponse.Places.Count > 0)
                {
                    _logger.LogInformation("Places API (New) v1 returned {Count} results for query: {Query}", searchResponse.Places.Count, query);
                    var v1Results = new List<PlaceDetails>();
                    foreach (var item in searchResponse.Places.Take(5))
                    {
                        v1Results.Add(MapNewToPlaceDetails(item));
                    }
                    return v1Results;
                }
            }
            else
            {
                var errContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Places API (New) v1 returned status code {StatusCode}: {Response}", response.StatusCode, errContent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Places API (New) v1 call encountered exception. Falling back to Legacy Places API.");
        }

        // 2. Legacy Places API Fallback
        try
        {
            var requestUrl = $"{_baseUrl.TrimEnd('/')}/textsearch/json?query={Uri.EscapeDataString(query)}&key={_apiKey}";
            _logger.LogInformation("Calling Legacy Google Places Text Search API for query: {Query}", query);

            var response = await _httpClient.GetAsync(requestUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Google Places Text Search API HTTP Error: {StatusCode}", response.StatusCode);
                return new List<PlaceDetails>();
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var searchResponse = JsonSerializer.Deserialize<GooglePlacesSearchResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (searchResponse == null || searchResponse.Results == null || searchResponse.Results.Count == 0)
            {
                _logger.LogInformation("Legacy Google Places API returned 0 results. Status: {Status}, ErrorMessage: {Error}", 
                    searchResponse?.Status, searchResponse?.ErrorMessage);
                return new List<PlaceDetails>();
            }

            var results = new List<PlaceDetails>();
            foreach (var item in searchResponse.Results.Take(5))
            {
                results.Add(MapToPlaceDetails(item));
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute Legacy Google Places Text Search for query: {Query}", query);
            return new List<PlaceDetails>();
        }
    }

    public async Task<List<PlaceDetails>> NearbySearchAsync(string location, string type = "", int radius = 5000, CancellationToken cancellationToken = default)
    {
        return await TextSearchAsync(location, cancellationToken);
    }

    public async Task<PlaceDetails?> GetPlaceDetailsAsync(string placeId, CancellationToken cancellationToken = default)
    {
        var results = await TextSearchAsync(placeId, cancellationToken);
        return results.FirstOrDefault();
    }

    public string GetPhotoUrl(string photoReference, int maxWidth = 400)
    {
        if (string.IsNullOrWhiteSpace(photoReference) || string.IsNullOrWhiteSpace(_apiKey))
        {
            return string.Empty;
        }

        if (photoReference.StartsWith("places/"))
        {
            return $"https://places.googleapis.com/v1/{photoReference}/media?maxHeightPx={maxWidth}&maxWidthPx={maxWidth}&key={_apiKey}";
        }

        return $"{_baseUrl.TrimEnd('/')}/photo?maxwidth={maxWidth}&photo_reference={Uri.EscapeDataString(photoReference)}&key={_apiKey}";
    }

    private PlaceDetails MapNewToPlaceDetails(PlaceNewApiItem item)
    {
        var name = item.DisplayName?.Text ?? "مكان سياحي";
        var address = item.FormattedAddress ?? "عنوان غير متوفر";
        var mapsLink = item.GoogleMapsUri ?? $"https://www.google.com/maps/search/?api=1&query={Uri.EscapeDataString(name)}";
        var description = item.EditorialSummary?.Text ?? "مكان مميز ووجهة سياحية.";

        string? photoUrl = null;
        if (item.Photos != null && item.Photos.Count > 0 && !string.IsNullOrWhiteSpace(item.Photos[0].Name))
        {
            photoUrl = GetPhotoUrl(item.Photos[0].Name!);
        }

        return new PlaceDetails
        {
            PlaceId = item.Id ?? string.Empty,
            Name = name,
            Rating = item.Rating,
            UserRatingsTotal = item.UserRatingCount,
            Address = address,
            Description = description,
            GoogleMapsLink = mapsLink,
            PhotoUrl = photoUrl,
            Types = item.Types ?? new List<string>()
        };
    }

    private PlaceDetails MapToPlaceDetails(GooglePlacesApiResult item)
    {
        var photoUrl = (item.Photos != null && item.Photos.Count > 0 && !string.IsNullOrWhiteSpace(item.Photos[0].PhotoReference))
            ? GetPhotoUrl(item.Photos[0].PhotoReference)
            : null;

        var mapsLink = !string.IsNullOrWhiteSpace(item.PlaceId)
            ? $"https://www.google.com/maps/place/?q=place_id:{item.PlaceId}"
            : $"https://www.google.com/maps/search/?api=1&query={Uri.EscapeDataString(item.Name)}";

        var typesDesc = item.Types != null && item.Types.Count > 0
            ? string.Join(", ", item.Types.Where(t => t != "point_of_interest" && t != "establishment").Take(3))
            : string.Empty;

        var description = item.EditorialSummary?.Overview 
            ?? (!string.IsNullOrWhiteSpace(typesDesc) ? $"Place type: {typesDesc}" : "A popular spot and point of interest.");

        return new PlaceDetails
        {
            PlaceId = item.PlaceId ?? string.Empty,
            Name = item.Name ?? "Unknown Place",
            Rating = item.Rating,
            UserRatingsTotal = item.UserRatingsTotal,
            Address = item.FormattedAddress ?? item.Vicinity ?? "Address not provided",
            Description = description,
            GoogleMapsLink = mapsLink,
            PhotoUrl = photoUrl,
            Types = item.Types ?? new List<string>()
        };
    }
}
