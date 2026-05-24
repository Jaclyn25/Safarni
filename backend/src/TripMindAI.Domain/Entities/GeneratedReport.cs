using TripMindAI.Domain.Enums;

namespace TripMindAI.Domain.Entities;

public class GeneratedReport
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public Guid? TripPlanId { get; set; }
    public TripPlan? TripPlan { get; set; }
    public Guid? VisaApplicationId { get; set; }
    public VisaApplication? VisaApplication { get; set; }
    public ReportType ReportType { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
