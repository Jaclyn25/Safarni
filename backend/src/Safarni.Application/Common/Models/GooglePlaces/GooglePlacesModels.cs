using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ChatBotApi.Application.Common.Models.GooglePlaces;

public class PlaceDetails
{
    public string PlaceId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double? Rating { get; set; }
    public int? UserRatingsTotal { get; set; }
    public string Address { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string GoogleMapsLink { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public List<string> Types { get; set; } = new();
}

public class PlacePhoto
{
    [JsonPropertyName("photo_reference")]
    public string PhotoReference { get; set; } = string.Empty;

    [JsonPropertyName("height")]
    public int Height { get; set; }

    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("html_attributions")]
    public List<string> HtmlAttributions { get; set; } = new();
}

public class GooglePlacesApiResult
{
    [JsonPropertyName("place_id")]
    public string PlaceId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("rating")]
    public double? Rating { get; set; }

    [JsonPropertyName("user_ratings_total")]
    public int? UserRatingsTotal { get; set; }

    [JsonPropertyName("formatted_address")]
    public string? FormattedAddress { get; set; }

    [JsonPropertyName("vicinity")]
    public string? Vicinity { get; set; }

    [JsonPropertyName("photos")]
    public List<PlacePhoto>? Photos { get; set; }

    [JsonPropertyName("types")]
    public List<string>? Types { get; set; }

    [JsonPropertyName("editorial_summary")]
    public EditorialSummary? EditorialSummary { get; set; }
}

public class EditorialSummary
{
    [JsonPropertyName("overview")]
    public string? Overview { get; set; }
}

public class GooglePlacesSearchResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("results")]
    public List<GooglePlacesApiResult>? Results { get; set; }

    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; set; }
}

public class GooglePlacesDetailsResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("result")]
    public GooglePlacesApiResult? Result { get; set; }

    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; set; }
}

// Places API (New) v1 Models
public class PlacesNewSearchResponse
{
    [JsonPropertyName("places")]
    public List<PlaceNewApiItem>? Places { get; set; }
}

public class PlaceNewApiItem
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("displayName")]
    public DisplayNameNew? DisplayName { get; set; }

    [JsonPropertyName("formattedAddress")]
    public string? FormattedAddress { get; set; }

    [JsonPropertyName("rating")]
    public double? Rating { get; set; }

    [JsonPropertyName("userRatingCount")]
    public int? UserRatingCount { get; set; }

    [JsonPropertyName("googleMapsUri")]
    public string? GoogleMapsUri { get; set; }

    [JsonPropertyName("types")]
    public List<string>? Types { get; set; }

    [JsonPropertyName("editorialSummary")]
    public DisplayNameNew? EditorialSummary { get; set; }

    [JsonPropertyName("photos")]
    public List<PlacePhotoNew>? Photos { get; set; }
}

public class DisplayNameNew
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("languageCode")]
    public string? LanguageCode { get; set; }
}

public class PlacePhotoNew
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}
