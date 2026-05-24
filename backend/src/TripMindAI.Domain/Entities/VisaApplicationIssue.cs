using TripMindAI.Domain.Enums;

namespace TripMindAI.Domain.Entities;

public class VisaApplicationIssue
{
    public Guid Id { get; set; }
    public Guid VisaApplicationId { get; set; }
    public VisaApplication VisaApplication { get; set; } = null!;
    public VisaIssueType IssueType { get; set; }
    public VisaIssueSeverity Severity { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsResolved { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
