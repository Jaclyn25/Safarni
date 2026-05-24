using TripMindAI.Domain.Enums;

namespace TripMindAI.Domain.Entities;

public class VisaRuleDocument
{
    public Guid Id { get; set; }
    public Guid VisaRuleId { get; set; }
    public VisaRule VisaRule { get; set; } = null!;
    public DocumentType DocumentType { get; set; }
    public bool IsRequired { get; set; }
    public int Weight { get; set; }
    public string? Notes { get; set; }
}
