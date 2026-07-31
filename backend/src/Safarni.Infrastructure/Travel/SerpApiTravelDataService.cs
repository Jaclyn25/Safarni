using System.Globalization;
using System.Text.Json;
using ChatBotApi.Application.Common.Models.GooglePlaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Safarni.Application.Abstractions.Travel;

namespace Safarni.Infrastructure.Travel;

internal sealed class SerpApiTravelDataService
    : IItineraryTravelDataService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SerpApiTravelDataService> _logger;
    private readonly string _apiKey;
    private readonly string _baseUrl;

    public SerpApiTravelDataService(
        HttpClient httpClient,
        IConfiguration configuration,
        IMemoryCache cache,
        ILogger<SerpApiTravelDataService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
        _apiKey = configuration["SerpApi:ApiKey"] ?? string.Empty;
        _baseUrl = configuration["SerpApi:BaseUrl"]
            ?? "https://serpapi.com/search.json";
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_apiKey);

    public async Task<IReadOnlyList<PlaceDetails>> SearchLocalAsync(
        string query,
        string destination,
        string languageCode,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return [];
        }

        var cacheKey = string.Join(
            ':',
            "serp-local",
            query.Trim().ToLowerInvariant(),
            destination.Trim().ToLowerInvariant(),
            NormalizeLanguage(languageCode));

        if (_cache.TryGetValue(
                cacheKey,
                out IReadOnlyList<PlaceDetails>? cached) &&
            cached is not null)
        {
            return cached;
        }

        var requestUri = BuildUrl(
            ("engine", "google_maps"),
            ("type", "search"),
            ("q", query),
            ("hl", NormalizeLanguage(languageCode)),
            ("output", "json"));

        try
        {
            using var response = await _httpClient.GetAsync(
                requestUri,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "SerpApi local search returned HTTP {StatusCode}.",
                    response.StatusCode);
                return [];
            }

            await using var stream =
                await response.Content.ReadAsStreamAsync(
                    cancellationToken);

            using var document =
                await JsonDocument.ParseAsync(
                    stream,
                    cancellationToken: cancellationToken);

            if (!TryGetArray(
                    document.RootElement,
                    "local_results",
                    out var results))
            {
                return [];
            }

            var mappedResults = results
                .EnumerateArray()
                .Select(result =>
                    MapLocalResult(
                        result,
                        destination,
                        query))
                .Where(place =>
                    !string.IsNullOrWhiteSpace(place.Name))
                .Take(20)
                .ToList();

            _cache.Set(
                cacheKey,
                mappedResults,
                TimeSpan.FromMinutes(30));

            return mappedResults;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "SerpApi local search failed for {Destination}.",
                destination);
            return [];
        }
    }

    public async Task<IReadOnlyList<PlaceDetails>> SearchHotelsAsync(
        string destination,
        string accommodationStyle,
        DateTime checkInDate,
        DateTime checkOutDate,
        int travelerCount,
        string currencyCode,
        string languageCode,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return [];
        }

        var effectiveCheckOut = checkOutDate.Date <= checkInDate.Date
            ? checkInDate.Date.AddDays(1)
            : checkOutDate.Date;

        var cacheKey = string.Join(
            ':',
            "serp-hotels",
            destination.Trim().ToLowerInvariant(),
            accommodationStyle.Trim().ToLowerInvariant(),
            checkInDate.ToString(
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture),
            effectiveCheckOut.ToString(
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture),
            travelerCount,
            currencyCode.Trim().ToUpperInvariant(),
            NormalizeLanguage(languageCode));

        if (_cache.TryGetValue(
                cacheKey,
                out IReadOnlyList<PlaceDetails>? cached) &&
            cached is not null)
        {
            return cached;
        }

        var requestUri = BuildUrl(
            ("engine", "google_hotels"),
            ("q", BuildHotelQuery(
                destination,
                accommodationStyle)),
            ("check_in_date", checkInDate.ToString(
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture)),
            ("check_out_date", effectiveCheckOut.ToString(
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture)),
            ("adults", travelerCount.ToString(
                CultureInfo.InvariantCulture)),
            ("children", "0"),
            ("currency", currencyCode.ToUpperInvariant()),
            ("hl", NormalizeLanguage(languageCode)),
            ("output", "json"));

        try
        {
            using var response = await _httpClient.GetAsync(
                requestUri,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "SerpApi hotel search returned HTTP {StatusCode}.",
                    response.StatusCode);
                return [];
            }

            await using var stream =
                await response.Content.ReadAsStreamAsync(
                    cancellationToken);

            using var document =
                await JsonDocument.ParseAsync(
                    stream,
                    cancellationToken: cancellationToken);

            if (!TryGetArray(
                    document.RootElement,
                    "properties",
                    out var properties))
            {
                return [];
            }

            var numberOfNights = Math.Max(
                1,
                (effectiveCheckOut - checkInDate.Date).Days);

            var availableProperties =
                properties.EnumerateArray().ToList();

            var preferredProperties =
                availableProperties
                    .Where(property =>
                        MatchesAccommodationStyle(
                            property,
                            accommodationStyle))
                    .ToList();

            var propertiesToMap =
                preferredProperties.Count > 0
                    ? preferredProperties
                    : availableProperties;

            var mappedProperties = propertiesToMap
                .Select(property =>
                    MapHotelResult(
                        property,
                        destination,
                        currencyCode,
                        numberOfNights))
                .Where(place =>
                    !string.IsNullOrWhiteSpace(place.Name))
                .OrderBy(place =>
                    place.TotalRate is null ? 1 : 0)
                .ThenBy(place =>
                    place.TotalRate ?? decimal.MaxValue)
                .ThenByDescending(place =>
                    place.Rating ?? 0)
                .Take(12)
                .ToList();

            _logger.LogInformation(
                "SerpApi hotel search for {Destination} returned {PropertyCount} properties and {UsableCount} usable {AccommodationStyle} hotel results.",
                destination,
                availableProperties.Count,
                mappedProperties.Count,
                accommodationStyle);

            _cache.Set(
                cacheKey,
                mappedProperties,
                TimeSpan.FromMinutes(30));

            return mappedProperties;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "SerpApi hotel search failed for {Destination}.",
                destination);
            return [];
        }
    }

    private PlaceDetails MapLocalResult(
        JsonElement result,
        string destination,
        string query)
    {
        var name = GetString(result, "title") ?? string.Empty;
        var address = GetString(result, "address");
        var type = GetString(result, "type");
        var placeId = GetString(result, "place_id")
            ?? GetString(result, "data_cid")
            ?? name;

        var latitude = GetNestedDouble(
            result,
            "gps_coordinates",
            "latitude");
        var longitude = GetNestedDouble(
            result,
            "gps_coordinates",
            "longitude");

        var photoUrl =
            GetString(result, "serpapi_thumbnail")
            ?? GetString(result, "thumbnail_large")
            ?? GetString(result, "thumbnail");

        var externalUrl =
            GetNestedString(result, "links", "website")
            ?? GetString(result, "website")
            ?? GetString(result, "booking_link")
            ?? GetString(result, "order_online")
            ?? GetNestedString(result, "links", "directions");

        var completeAddress =
            string.IsNullOrWhiteSpace(address)
                ? destination
                : address;

        return new PlaceDetails
        {
            PlaceId = $"serp-local:{placeId}",
            Name = name,
            Rating = GetDouble(result, "rating"),
            UserRatingsTotal = GetInt(result, "reviews"),
            Address = completeAddress,
            Description =
                GetString(result, "description")
                ?? type
                ?? string.Empty,
            GoogleMapsLink = BuildMapsPlaceUrl(
                name,
                completeAddress,
                placeId,
                latitude,
                longitude),
            PhotoUrl = photoUrl,
            Types = MapLocalTypes(
                type,
                query),
            Latitude = latitude,
            Longitude = longitude,
            PriceLevel = GetString(result, "price"),
            ExternalUrl = externalUrl,
            DataSource = "serpapi_google_maps"
        };
    }

    private static PlaceDetails MapHotelResult(
        JsonElement property,
        string destination,
        string currencyCode,
        int numberOfNights)
    {
        var name = GetString(property, "name") ?? string.Empty;
        var propertyToken =
            GetString(property, "property_token") ?? name;

        var latitude = GetNestedDouble(
            property,
            "gps_coordinates",
            "latitude");
        var longitude = GetNestedDouble(
            property,
            "gps_coordinates",
            "longitude");

        var listedNightlyRate = GetNestedDecimal(
            property,
            "rate_per_night",
            "extracted_lowest");

        var totalRate = GetNestedDecimal(
            property,
            "total_rate",
            "extracted_lowest")
            ?? listedNightlyRate * numberOfNights;

        var nightlyRate = totalRate is not null
            ? decimal.Round(
                totalRate.Value / numberOfNights,
                2,
                MidpointRounding.AwayFromZero)
            : listedNightlyRate;

        var photoUrl = GetFirstImage(property);
        var priceSource = GetFirstPriceSource(property);

        return new PlaceDetails
        {
            PlaceId = $"serp-hotel:{propertyToken}",
            Name = name,
            Rating =
                GetDouble(property, "overall_rating")
                ?? GetDouble(property, "rating"),
            UserRatingsTotal = GetInt(property, "reviews"),
            Address = destination,
            Description = BuildHotelDescription(property),
            GoogleMapsLink = BuildMapsPlaceUrl(
                name,
                destination,
                null,
                latitude,
                longitude),
            PhotoUrl = photoUrl,
            Types = ["lodging", "hotel"],
            Latitude = latitude,
            Longitude = longitude,
            RatePerNight = nightlyRate,
            TotalRate = totalRate,
            PriceCurrency = currencyCode.ToUpperInvariant(),
            PriceSource = priceSource,
            ExternalUrl =
                GetFirstPriceLink(property)
                ?? GetString(property, "link"),
            DataSource = "serpapi_hotels"
        };
    }

    private string BuildUrl(
        params (string Key, string Value)[] values)
    {
        var query = values
            .Append((Key: "api_key", Value: _apiKey))
            .Where(value =>
                !string.IsNullOrWhiteSpace(value.Value))
            .Select(value =>
                $"{Uri.EscapeDataString(value.Key)}=" +
                Uri.EscapeDataString(value.Value));

        return $"{_baseUrl.TrimEnd('?')}?{string.Join("&", query)}";
    }

    private static string BuildHotelQuery(
        string destination,
        string accommodationStyle)
    {
        var style = accommodationStyle
            .Trim()
            .ToLowerInvariant() switch
            {
                "budget" => "budget",
                "mid_range" => "mid-range",
                "luxury" => "luxury",
                "boutique" => "boutique",
                "resort" => "resort",
                _ => string.Empty
            };

        return string.IsNullOrWhiteSpace(style)
            ? $"hotels in {destination}"
            : $"{style} hotels in {destination}";
    }

    private static bool MatchesAccommodationStyle(
        JsonElement property,
        string accommodationStyle)
    {
        var style = accommodationStyle
            .Trim()
            .ToLowerInvariant();

        var hotelClass = GetHotelClass(property);

        var name =
            GetString(property, "name")
            ?? string.Empty;

        var type =
            GetString(property, "type")
            ?? string.Empty;

        return style switch
        {
            "budget" =>
                hotelClass is null or <= 3,
            "mid_range" =>
                hotelClass is >= 3 and <= 4,
            "luxury" =>
                hotelClass is >= 4,
            "boutique" =>
                name.Contains(
                    "boutique",
                    StringComparison.OrdinalIgnoreCase) ||
                type.Contains(
                    "boutique",
                    StringComparison.OrdinalIgnoreCase),
            "resort" =>
                name.Contains(
                    "resort",
                    StringComparison.OrdinalIgnoreCase) ||
                type.Contains(
                    "resort",
                    StringComparison.OrdinalIgnoreCase),
            _ => true
        };
    }

    private static string NormalizeLanguage(
        string languageCode)
    {
        return string.Equals(
            languageCode,
            "ar",
            StringComparison.OrdinalIgnoreCase)
                ? "ar"
                : "en";
    }

    private static string BuildMapsPlaceUrl(
        string name,
        string? address,
        string? externalPlaceId,
        double? latitude,
        double? longitude)
    {
        if (IsNumericPlaceId(externalPlaceId))
        {
            return "https://www.google.com/maps?cid="
                   + Uri.EscapeDataString(
                       externalPlaceId!);
        }

        var namedQuery = string.Join(
            ", ",
            new[] { name, address }
                .Where(value =>
                    !string.IsNullOrWhiteSpace(value))
                .Distinct(
                    StringComparer.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(namedQuery))
        {
            var url =
                "https://www.google.com/maps/search/?api=1&query="
                + Uri.EscapeDataString(namedQuery);

            if (IsGooglePlaceId(externalPlaceId))
            {
                url += "&query_place_id="
                       + Uri.EscapeDataString(
                           externalPlaceId!);
            }

            return url;
        }

        var coordinateQuery =
            latitude is not null &&
            longitude is not null
                ? string.Create(
                    CultureInfo.InvariantCulture,
                    $"{latitude},{longitude}")
                : string.Empty;

        return "https://www.google.com/maps/search/?api=1&query="
               + Uri.EscapeDataString(coordinateQuery);
    }

    private static bool IsNumericPlaceId(
        string? placeId)
    {
        return !string.IsNullOrWhiteSpace(placeId) &&
               placeId.All(char.IsDigit);
    }

    private static bool IsGooglePlaceId(
        string? placeId)
    {
        return !string.IsNullOrWhiteSpace(placeId) &&
               placeId.StartsWith(
                   "ChIJ",
                   StringComparison.Ordinal);
    }

    private static List<string> MapLocalTypes(
        string? type,
        string query)
    {
        var normalizedQuery =
            query.ToLowerInvariant();

        if (normalizedQuery.Contains("hotel") ||
            normalizedQuery.Contains("resort") ||
            normalizedQuery.Contains("lodging") ||
            normalizedQuery.Contains("accommodation"))
        {
            return ["lodging", "hotel"];
        }

        if (normalizedQuery.Contains("cafe") ||
            normalizedQuery.Contains("coffee") ||
            normalizedQuery.Contains("breakfast"))
        {
            return ["cafe", "food"];
        }

        if (normalizedQuery.Contains("restaurant") ||
            normalizedQuery.Contains("dinner") ||
            normalizedQuery.Contains("lunch") ||
            normalizedQuery.Contains("food"))
        {
            return ["restaurant", "food"];
        }

        if (string.IsNullOrWhiteSpace(type))
        {
            return ["point_of_interest"];
        }

        var normalized = type.ToLowerInvariant();

        if (normalized.Contains("cafe") ||
            normalized.Contains("coffee") ||
            normalized.Contains("مقهى") ||
            normalized.Contains("قهوة"))
        {
            return ["cafe", "food"];
        }

        if (normalized.Contains("restaurant") ||
            normalized.Contains("food") ||
            normalized.Contains("bakery") ||
            normalized.Contains("مطعم") ||
            normalized.Contains("مخبز"))
        {
            return ["restaurant", "food"];
        }

        if (normalized.Contains("hotel") ||
            normalized.Contains("lodging") ||
            normalized.Contains("resort") ||
            normalized.Contains("فندق") ||
            normalized.Contains("منتجع"))
        {
            return ["lodging", "hotel"];
        }

        return ["point_of_interest"];
    }

    private static string BuildHotelDescription(
        JsonElement property)
    {
        var hotelClass = GetHotelClass(property);

        return hotelClass is null
            ? "Date-specific hotel result."
            : $"{hotelClass}-star date-specific hotel result.";
    }

    private static int? GetHotelClass(
        JsonElement property)
    {
        var numericClass =
            GetInt(property, "hotel_class");

        if (numericClass is not null)
        {
            return numericClass;
        }

        var textClass =
            GetString(property, "hotel_class");

        if (string.IsNullOrWhiteSpace(textClass))
        {
            return null;
        }

        var firstDigit = textClass.FirstOrDefault(
            character => char.IsDigit(character));

        return firstDigit == default
            ? null
            : firstDigit - '0';
    }

    private static string? GetFirstImage(
        JsonElement property)
    {
        if (!TryGetArray(
                property,
                "images",
                out var images))
        {
            return GetString(property, "thumbnail");
        }

        var first = images.EnumerateArray().FirstOrDefault();

        return first.ValueKind == JsonValueKind.Object
            ? GetString(first, "thumbnail")
              ?? GetString(first, "original_image")
            : null;
    }

    private static string? GetFirstPriceSource(
        JsonElement property)
    {
        if (!TryGetArray(
                property,
                "prices",
                out var prices))
        {
            return null;
        }

        var first = prices.EnumerateArray().FirstOrDefault();

        return first.ValueKind == JsonValueKind.Object
            ? GetString(first, "source")
            : null;
    }

    private static string? GetFirstPriceLink(
        JsonElement property)
    {
        if (!TryGetArray(
                property,
                "prices",
                out var prices))
        {
            return null;
        }

        return prices
            .EnumerateArray()
            .Where(price =>
                price.ValueKind == JsonValueKind.Object)
            .Select(price =>
                GetString(price, "link"))
            .FirstOrDefault(link =>
                !string.IsNullOrWhiteSpace(link));
    }

    private static bool TryGetArray(
        JsonElement element,
        string propertyName,
        out JsonElement array)
    {
        return element.TryGetProperty(
                   propertyName,
                   out array) &&
               array.ValueKind == JsonValueKind.Array;
    }

    private static string? GetString(
        JsonElement element,
        string propertyName)
    {
        return element.TryGetProperty(
                   propertyName,
                   out var value) &&
               value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static string? GetNestedString(
        JsonElement element,
        string objectName,
        string propertyName)
    {
        return element.TryGetProperty(
                   objectName,
                   out var nested) &&
               nested.ValueKind == JsonValueKind.Object
            ? GetString(nested, propertyName)
            : null;
    }

    private static double? GetDouble(
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
            value.TryGetDouble(out var numericResult))
        {
            return numericResult;
        }

        return value.ValueKind == JsonValueKind.String &&
               double.TryParse(
                   value.GetString(),
                   NumberStyles.Any,
                   CultureInfo.InvariantCulture,
                   out var textResult)
            ? textResult
            : null;
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
            value.TryGetInt32(out var numericResult))
        {
            return numericResult;
        }

        if (value.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        var text = value.GetString();

        if (int.TryParse(
                text,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var exactResult))
        {
            return exactResult;
        }

        var firstDigit = text?.FirstOrDefault(
            char.IsDigit);

        return firstDigit == default
            ? null
            : firstDigit - '0';
    }

    private static double? GetNestedDouble(
        JsonElement element,
        string objectName,
        string propertyName)
    {
        return element.TryGetProperty(
                   objectName,
                   out var nested) &&
               nested.ValueKind == JsonValueKind.Object
            ? GetDouble(nested, propertyName)
            : null;
    }

    private static decimal? GetNestedDecimal(
        JsonElement element,
        string objectName,
        string propertyName)
    {
        if (!element.TryGetProperty(
                objectName,
                out var nested) ||
            nested.ValueKind != JsonValueKind.Object ||
            !nested.TryGetProperty(
                propertyName,
                out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number &&
            value.TryGetDecimal(out var numericResult))
        {
            return numericResult;
        }

        if (value.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        var normalized = new string(
            (value.GetString() ?? string.Empty)
            .Where(character =>
                char.IsDigit(character) ||
                character is '.' or '-' or ',')
            .ToArray())
            .Replace(",", string.Empty);

        return decimal.TryParse(
            normalized,
            NumberStyles.Any,
            CultureInfo.InvariantCulture,
            out var textResult)
            ? textResult
            : null;
    }
}
