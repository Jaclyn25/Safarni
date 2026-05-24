using TripMindAI.Domain.Enums;

namespace TripMindAI.Domain.Entities;

public class VisaAlert
{
    public Guid Id { get; set; }
    public string CountryFrom { get; set; } = "Egypt";
    public string CountryTo { get; set; } = string.Empty;
    public VisaAlertType AlertType { get; set; }
    public VisaAlertSeverity Severity { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? SourceUrl { get; set; }
    public bool IsActive { get; set; }
    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
}
