using TripMindAI.Domain.Enums;

namespace TripMindAI.Domain.Entities;

public class Destination
{
    public Guid Id { get; set; }
    public string Country { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal? AverageDailyBudgetUsd { get; set; }
    public string? BestSeason { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    public ICollection<PointOfInterest> PointsOfInterest { get; set; } = new List<PointOfInterest>();
}
