using TripMindAI.Domain.Enums;

namespace TripMindAI.Domain.Entities;

public class TripCostItem
{
    public Guid Id { get; set; }
    public Guid TripPlanId { get; set; }
    public TripPlan TripPlan { get; set; } = null!;
    public CostCategory Category { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal AmountUsd { get; set; }
    public decimal? AmountEgp { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
