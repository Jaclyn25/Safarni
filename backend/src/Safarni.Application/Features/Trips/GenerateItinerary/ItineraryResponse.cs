using Safarni.Domain.Entities;

namespace Safarni.Application.Features.Trips.GenerateItinerary;

public sealed record ItineraryResponse(
    Guid Id,
    Guid TripId,
    GenerateItineraryPreferences Preferences,
    decimal? BudgetAmount,
    string CurrencyCode,
    int TravelerCount,
    string Status,
    string GeneratedBy,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    decimal EstimatedTotalCost,
    int UnknownCostItemCount,
    bool IsCompleteCostEstimate,
    IReadOnlyList<ItineraryDayResponse> Days);

public sealed record ItineraryDayResponse(
    Guid Id,
    int DayNumber,
    string Date,
    string Title,
    string? Notes,
    decimal EstimatedTotalCost,
    int UnknownCostItemCount,
    bool IsCompleteCostEstimate,
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
    string? FlightDepartureAirportCode,
    string? FlightDepartureAirportName,
    string? FlightArrivalAirportCode,
    string? FlightArrivalAirportName,
    string? FlightAirlines,
    string? FlightNumbers,
    int? FlightDurationMinutes,
    int? FlightStopCount,
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
        var unknownCostCount = days.Sum(day => day.UnknownCostItemCount);

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
            unknownCostCount,
            unknownCostCount == 0,
            days);
    }

    public static ItineraryDayResponse MapDay(ItineraryDay day)
    {
        var items = day.Items
            .OrderBy(item => item.OrderIndex)
            .Select(MapItem)
            .ToList();
        var unknownCostCount = items.Count(item =>
            item.EstimatedCost is null);

        return new ItineraryDayResponse(
            day.Id,
            day.DayNumber,
            day.Date.ToString("yyyy-MM-dd"),
            day.Title,
            day.Notes,
            items.Sum(item => item.EstimatedCost ?? 0),
            unknownCostCount,
            unknownCostCount == 0,
            items);
    }

    private static ItineraryItemResponse MapItem(ItineraryItem item)
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
            item.StayNights,
            item.PriceSource,
            item.ExternalUrl,
            item.CostSource,
            item.FlightDepartureAirportCode,
            item.FlightDepartureAirportName,
            item.FlightArrivalAirportCode,
            item.FlightArrivalAirportName,
            item.FlightAirlines,
            item.FlightNumbers,
            item.FlightDurationMinutes,
            item.FlightStopCount,
            item.RouteDistanceMeters,
            item.RouteDurationMinutes,
            item.RouteTravelMode,
            item.RouteTransitDetails,
            item.Category.ToString().ToLowerInvariant(),
            item.EstimatedCost,
            item.OrderIndex);
    }

    private static string? ResolveGoogleMapsUrl(ItineraryItem item)
    {
        const string serpLocalPrefix = "serp-local:";

        if (!string.IsNullOrWhiteSpace(item.GooglePlaceId) &&
            item.GooglePlaceId.StartsWith(
                serpLocalPrefix,
                StringComparison.OrdinalIgnoreCase))
        {
            var placeId = item.GooglePlaceId[serpLocalPrefix.Length..];

            if (placeId.Length > 0 && placeId.All(char.IsDigit))
            {
                return "https://www.google.com/maps?cid=" +
                       Uri.EscapeDataString(placeId);
            }
        }

        return item.GoogleMapsUrl;
    }

    private static string? FormatTime(TimeSpan? value)
    {
        return value?.ToString(@"hh\:mm");
    }
}
