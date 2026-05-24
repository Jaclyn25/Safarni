using TripMindAI.Domain.Enums;

namespace TripMindAI.Domain.Entities;

public class CurrencyRate
{
    public Guid Id { get; set; }
    public string BaseCurrency { get; set; } = "USD";
    public string TargetCurrency { get; set; } = "EGP";
    public decimal Rate { get; set; }
    public ExternalProvider Provider { get; set; }
    public DateTime RetrievedAt { get; set; } = DateTime.UtcNow;
}
