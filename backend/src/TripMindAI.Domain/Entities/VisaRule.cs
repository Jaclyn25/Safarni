using TripMindAI.Domain.Enums;

namespace TripMindAI.Domain.Entities;

public class VisaRule
{
    public Guid Id { get; set; }
    public string CountryFrom { get; set; } = "Egypt";
    public string CountryTo { get; set; } = string.Empty;
    public VisaType VisaType { get; set; }
    public int MinPassportValidityMonths { get; set; }
    public int? MinBlankPassportPages { get; set; }
    public string? Notes { get; set; }
    public string? OfficialSourceUrl { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    public ICollection<VisaRuleDocument> Documents { get; set; } = new List<VisaRuleDocument>();
}
