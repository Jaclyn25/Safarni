using TripMindAI.Domain.Enums;

namespace TripMindAI.Domain.Entities;

public class VisaApplication
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public string NationalityCountry { get; set; } = "Egypt";
    public string DestinationCountry { get; set; } = string.Empty;
    public VisaType VisaType { get; set; }
    public DateTime? TravelStartDate { get; set; }
    public DateTime? TravelEndDate { get; set; }
    public int? ReadinessScore { get; set; }
    public VisaApplicationStatus Status { get; set; }
    public VisaRiskLevel RiskLevel { get; set; }
    public string? Summary { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<UploadedDocument> UploadedDocuments { get; set; } = new List<UploadedDocument>();
    public ICollection<VisaApplicationIssue> Issues { get; set; } = new List<VisaApplicationIssue>();
    public ICollection<TripPlan> TripPlans { get; set; } = new List<TripPlan>();
    public ICollection<GeneratedReport> GeneratedReports { get; set; } = new List<GeneratedReport>();
}
