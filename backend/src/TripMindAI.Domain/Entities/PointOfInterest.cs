using TripMindAI.Domain.Enums;

namespace TripMindAI.Domain.Entities;

public class PointOfInterest
{
    public Guid Id { get; set; }
    public Guid? DestinationId { get; set; }
    public Destination? Destination { get; set; }
    public string Name { get; set; } = string.Empty;
    public PoiCategory Category { get; set; }
    public string? Address { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public decimal? Rating { get; set; }
    public string? ExternalPlaceId { get; set; }
    public ExternalProvider Provider { get; set; }
    public decimal? EstimatedCostUsd { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<SavedPlace> SavedByUsers { get; set; } = new List<SavedPlace>();
    public ICollection<TripActivity> TripActivities { get; set; } = new List<TripActivity>();
}
