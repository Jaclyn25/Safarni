using TripMindAI.Domain.Enums;

namespace TripMindAI.Domain.Entities;

public class TripActivity
{
    public Guid Id { get; set; }
    public Guid TripDayId { get; set; }
    public TripDay TripDay { get; set; } = null!;
    public Guid? PointOfInterestId { get; set; }
    public PointOfInterest? PointOfInterest { get; set; }
    public ActivityTimeSlot TimeSlot { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? LocationName { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public decimal? EstimatedCostUsd { get; set; }
    public decimal? EstimatedCostEgp { get; set; }
    public int SortOrder { get; set; }
    public TripActivityType ActivityType { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
