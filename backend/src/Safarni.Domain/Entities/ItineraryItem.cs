namespace Safarni.Domain.Entities;

public class ItineraryItem
{
    public Guid Id { get; set; }

    public Guid ItineraryDayId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public TimeSpan? StartTime { get; set; }

    public TimeSpan? EndTime { get; set; }

    public string? Location { get; set; }

    public string? GooglePlaceId { get; set; }

    public string? Address { get; set; }

    public string? GoogleMapsUrl { get; set; }

    public double? Rating { get; set; }

    public string? PhotoUrl { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public string? PriceLevel { get; set; }

    public decimal? RatePerNight { get; set; }

    public string? PriceSource { get; set; }

    public string? ExternalUrl { get; set; }

    public string? CostSource { get; set; }

    public int? RouteDistanceMeters { get; set; }

    public int? RouteDurationMinutes { get; set; }

    public string? RouteTravelMode { get; set; }

    public string? RouteTransitDetails { get; set; }

    public ItineraryItemCategory Category { get; set; } =
        ItineraryItemCategory.Activity;

    public decimal? EstimatedCost { get; set; }

    public int OrderIndex { get; set; }

    public ItineraryDay ItineraryDay { get; set; } = null!;
}
