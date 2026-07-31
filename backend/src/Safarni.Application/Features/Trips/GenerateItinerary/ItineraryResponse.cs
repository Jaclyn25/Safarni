using Safarni.Domain.Entities;

namespace Safarni.Application.Features.Trips.GenerateItinerary;

public sealed record ItineraryResponse(
    Guid Id,
    Guid TripId,
    GenerateItineraryPreferences Preferences,
    decimal BudgetAmount,
    string CurrencyCode,
    int TravelerCount,
    string Status,
    string GeneratedBy,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    decimal EstimatedTotalCost,
    IReadOnlyList<ItineraryDayResponse> Days);

public sealed record ItineraryDayResponse(
    Guid Id,
    int DayNumber,
    string Date,
    string Title,
    string? Notes,
    decimal EstimatedTotalCost,
    IReadOnlyList<ItineraryItemResponse> Items);

public sealed record ItineraryItemResponse(
    Guid Id,
    string Title,
    string? Description,
    string? StartTime,
    string? EndTime,
    string? Location,
    string? GooglePlaceId,
    string? Address,
    string? GoogleMapsUrl,
    double? Rating,
    string? PhotoUrl,
    string? PriceLevel,
    decimal? RatePerNight,
    int? StayNights,
    string? PriceSource,
    string? ExternalUrl,
    string? CostSource,
    int? RouteDistanceMeters,
    int? RouteDurationMinutes,
    string? RouteTravelMode,
    string? RouteTransitDetails,
    string Category,
    decimal? EstimatedCost,
    int OrderIndex);

internal static class ItineraryResponseMapper
{
    public static ItineraryResponse Map(Itinerary itinerary)
    {
        var days = itinerary.Days
            .OrderBy(day => day.DayNumber)
            .Select(MapDay)
            .ToList();

        return new ItineraryResponse(
            itinerary.Id,
            itinerary.TripId,
            GenerateItineraryPreferencesSerializer.Deserialize(
                itinerary.Preferences),
            itinerary.BudgetAmount,
            itinerary.CurrencyCode,
            itinerary.TravelerCount,
            itinerary.Status.ToString().ToLowerInvariant(),
            itinerary.GeneratedBy.ToString().ToLowerInvariant(),
            itinerary.IsActive,
            itinerary.CreatedAt,
            itinerary.UpdatedAt,
            days.Sum(day => day.EstimatedTotalCost),
            days);
    }

    public static ItineraryDayResponse MapDay(ItineraryDay day)
    {
        var items = day.Items
            .OrderBy(item => item.OrderIndex)
            .Select(MapItem)
            .ToList();

        return new ItineraryDayResponse(
            day.Id,
            day.DayNumber,
            day.Date.ToString("yyyy-MM-dd"),
            day.Title,
            day.Notes,
            items.Sum(item => item.EstimatedCost ?? 0),
            items);
    }

    private static ItineraryItemResponse MapItem(
        ItineraryItem item)
    {
        return new ItineraryItemResponse(
            item.Id,
            item.Title,
            item.Description,
            FormatTime(item.StartTime),
            FormatTime(item.EndTime),
            item.Location,
            item.GooglePlaceId,
            item.Address,
            ResolveGoogleMapsUrl(item),
            item.Rating,
            item.PhotoUrl,
            item.PriceLevel,
            item.RatePerNight,
            CalculateStayNights(item),
            item.PriceSource,
            item.ExternalUrl,
            item.CostSource,
            item.RouteDistanceMeters,
            item.RouteDurationMinutes,
            item.RouteTravelMode,
            item.RouteTransitDetails,
            item.Category.ToString().ToLowerInvariant(),
            item.EstimatedCost,
            item.OrderIndex);
    }

    private static int? CalculateStayNights(
        ItineraryItem item)
    {
        if (item.Category !=
                ItineraryItemCategory.Accommodation ||
            item.RatePerNight is null or <= 0 ||
            item.EstimatedCost is null or <= 0)
        {
            return null;
        }

        return Math.Max(
            1,
            (int)decimal.Round(
                item.EstimatedCost.Value /
                item.RatePerNight.Value,
                0,
                MidpointRounding.AwayFromZero));
    }

    private static string? ResolveGoogleMapsUrl(
        ItineraryItem item)
    {
        const string serpLocalPrefix = "serp-local:";

        if (!string.IsNullOrWhiteSpace(
                item.GooglePlaceId) &&
            item.GooglePlaceId.StartsWith(
                serpLocalPrefix,
                StringComparison.OrdinalIgnoreCase))
        {
            var placeId = item.GooglePlaceId[
                serpLocalPrefix.Length..];

            if (placeId.Length > 0 &&
                placeId.All(char.IsDigit))
            {
                return "https://www.google.com/maps?cid="
                       + Uri.EscapeDataString(placeId);
            }
        }

        return item.GoogleMapsUrl;
    }

    private static string? FormatTime(TimeSpan? value)
    {
        return value?.ToString(@"hh\:mm");
    }
}
