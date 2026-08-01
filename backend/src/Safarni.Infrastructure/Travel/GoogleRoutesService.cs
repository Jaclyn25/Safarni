using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Safarni.Application.Abstractions.Travel;

namespace Safarni.Infrastructure.Travel;

internal sealed class GoogleRoutesService
    : IItineraryRouteService
{
    private const string RoutesUrl =
        "https://routes.googleapis.com/directions/v2:computeRoutes";

    private readonly HttpClient _httpClient;
    private readonly ILogger<GoogleRoutesService> _logger;
    private readonly string _apiKey;

    public GoogleRoutesService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GoogleRoutesService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey =
            configuration["GoogleRoutes:ApiKey"]
            ?? string.Empty;
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_apiKey);

    public async Task<ItineraryRouteResult?>
        ComputeRouteAsync(
            ItineraryRoutePoint origin,
            ItineraryRoutePoint destination,
            IReadOnlyList<string> transportationPreferences,
            string languageCode,
            CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return null;
        }

        var travelMode = SelectTravelMode(
            origin,
            destination,
            transportationPreferences);

        var payload = new
        {
            origin = new
            {
                location = new
                {
                    latLng = new
                    {
                        latitude = origin.Latitude,
                        longitude = origin.Longitude
                    }
                }
            },
            destination = new
            {
                location = new
                {
                    latLng = new
                    {
                        latitude = destination.Latitude,
                        longitude = destination.Longitude
                    }
                }
            },
            travelMode,
            computeAlternativeRoutes = false,
            languageCode = NormalizeLanguage(languageCode),
            units = "METRIC"
        };

        var payloadJson = JsonSerializer.Serialize(payload);

        HttpRequestMessage CreateRequest()
        {
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                RoutesUrl);

            request.Headers.Add("X-Goog-Api-Key", _apiKey);
            request.Headers.Add(
                "X-Goog-FieldMask",
                "routes.distanceMeters,routes.duration," +
                "routes.legs.steps.travelMode," +
                "routes.legs.steps.transitDetails");

            request.Content = new StringContent(
                payloadJson,
                Encoding.UTF8,
                "application/json");

            return request;
        }

        try
        {
            using var response = await ExternalHttpResilience.SendAsync(
                _httpClient,
                CreateRequest,
                _logger,
                "GoogleRoutes",
                $"route calculation using {travelMode}",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Google Routes returned HTTP {StatusCode} for travel mode {TravelMode}.",
                    response.StatusCode,
                    travelMode);
                return null;
            }

            await using var stream =
                await response.Content.ReadAsStreamAsync(
                    cancellationToken);

            using var document =
                await JsonDocument.ParseAsync(
                    stream,
                    cancellationToken: cancellationToken);

            return ParseRoute(
                document.RootElement,
                travelMode);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Google Routes failed for travel mode {TravelMode}.",
                travelMode);
            return null;
        }
    }

    private static ItineraryRouteResult? ParseRoute(
        JsonElement root,
        string requestedTravelMode)
    {
        if (!root.TryGetProperty(
                "routes",
                out var routes) ||
            routes.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var route = routes.EnumerateArray().FirstOrDefault();

        if (route.ValueKind != JsonValueKind.Object ||
            !route.TryGetProperty(
                "distanceMeters",
                out var distanceElement) ||
            !distanceElement.TryGetInt32(
                out var distanceMeters))
        {
            return null;
        }

        var durationMinutes = ParseDurationMinutes(
            route.TryGetProperty(
                "duration",
                out var duration)
                ? duration.GetString()
                : null);

        return new ItineraryRouteResult(
            distanceMeters,
            durationMinutes,
            FormatTravelMode(requestedTravelMode),
            FindTransitDetails(route));
    }

    private static string? FindTransitDetails(
        JsonElement route)
    {
        if (!route.TryGetProperty("legs", out var legs) ||
            legs.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var details = new List<string>();

        foreach (var leg in legs.EnumerateArray())
        {
            if (!leg.TryGetProperty(
                    "steps",
                    out var steps) ||
                steps.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var step in steps.EnumerateArray())
            {
                if (!step.TryGetProperty(
                        "transitDetails",
                        out var transit) ||
                    transit.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var lineName = GetNestedString(
                    transit,
                    "transitLine",
                    "name");

                var vehicleType =
                    GetNestedNestedString(
                        transit,
                        "transitLine",
                        "vehicle",
                        "type");

                var label = string.Join(
                    " · ",
                    new[]
                    {
                        FormatTransitType(vehicleType),
                        lineName
                    }.Where(value =>
                        !string.IsNullOrWhiteSpace(value)));

                if (!string.IsNullOrWhiteSpace(label) &&
                    !details.Contains(
                        label,
                        StringComparer.OrdinalIgnoreCase))
                {
                    details.Add(label);
                }
            }
        }

        return details.Count == 0
            ? null
            : string.Join(" → ", details);
    }

    private static string SelectTravelMode(
        ItineraryRoutePoint origin,
        ItineraryRoutePoint destination,
        IReadOnlyList<string> preferences)
    {
        var normalized = preferences
            .Select(value =>
                value.Trim().ToLowerInvariant())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var directDistance = CalculateDirectDistanceMeters(
            origin,
            destination);

        if (normalized.Contains("mixed"))
        {
            return directDistance <= 1_800
                ? "WALK"
                : directDistance <= 40_000
                    ? "TRANSIT"
                    : "DRIVE";
        }

        if (normalized.Contains("walking") &&
            directDistance <= 2_000)
        {
            return "WALK";
        }

        if (normalized.Contains("bicycle") &&
            directDistance <= 8_000)
        {
            return "BICYCLE";
        }

        if (normalized.Contains("public_transport"))
        {
            return "TRANSIT";
        }

        if (normalized.Contains("taxi_ride_share") ||
            normalized.Contains("rental_car"))
        {
            return "DRIVE";
        }

        if (normalized.Contains("bicycle"))
        {
            return "BICYCLE";
        }

        return normalized.Contains("walking")
            ? "WALK"
            : "DRIVE";
    }

    private static double CalculateDirectDistanceMeters(
        ItineraryRoutePoint origin,
        ItineraryRoutePoint destination)
    {
        const double earthRadius = 6_371_000;

        var latitude1 = DegreesToRadians(origin.Latitude);
        var latitude2 = DegreesToRadians(destination.Latitude);
        var latitudeDelta =
            DegreesToRadians(
                destination.Latitude - origin.Latitude);
        var longitudeDelta =
            DegreesToRadians(
                destination.Longitude - origin.Longitude);

        var a =
            Math.Sin(latitudeDelta / 2) *
            Math.Sin(latitudeDelta / 2) +
            Math.Cos(latitude1) *
            Math.Cos(latitude2) *
            Math.Sin(longitudeDelta / 2) *
            Math.Sin(longitudeDelta / 2);

        return earthRadius *
               2 *
               Math.Atan2(
                   Math.Sqrt(a),
                   Math.Sqrt(1 - a));
    }

    private static double DegreesToRadians(double value)
    {
        return value * Math.PI / 180;
    }

    private static int ParseDurationMinutes(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            !double.TryParse(
                value.TrimEnd('s'),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var seconds))
        {
            return 0;
        }

        return Math.Max(
            1,
            (int)Math.Ceiling(seconds / 60));
    }

    private static string FormatTravelMode(string mode)
    {
        return mode switch
        {
            "WALK" => "walking",
            "BICYCLE" => "bicycle",
            "TRANSIT" => "public_transport",
            _ => "driving"
        };
    }

    private static string? FormatTransitType(
        string? value)
    {
        return value?.ToUpperInvariant() switch
        {
            "BUS" => "Bus",
            "SUBWAY" or "METRO_RAIL" => "Subway",
            "TRAIN" or "HEAVY_RAIL" or "COMMUTER_TRAIN" =>
                "Train",
            "LIGHT_RAIL" or "TRAM" => "Tram",
            "FERRY" => "Ferry",
            _ => value?.Replace('_', ' ').ToLowerInvariant()
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

    private static string? GetNestedString(
        JsonElement element,
        string objectName,
        string propertyName)
    {
        return element.TryGetProperty(
                   objectName,
                   out var nested) &&
               nested.ValueKind == JsonValueKind.Object &&
               nested.TryGetProperty(
                   propertyName,
                   out var value) &&
               value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static string? GetNestedNestedString(
        JsonElement element,
        string firstObject,
        string secondObject,
        string propertyName)
    {
        return element.TryGetProperty(
                   firstObject,
                   out var first) &&
               first.ValueKind == JsonValueKind.Object &&
               first.TryGetProperty(
                   secondObject,
                   out var second) &&
               second.ValueKind == JsonValueKind.Object &&
               second.TryGetProperty(
                   propertyName,
                   out var value) &&
               value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }
}
