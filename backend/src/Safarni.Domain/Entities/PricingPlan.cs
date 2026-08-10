using Safarni.Domain.Enums;

namespace Safarni.Domain.Entities;

public sealed class PricingPlan
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public PricingCustomerType CustomerType { get; set; }
    public BillingPeriod BillingPeriod { get; set; }
    public decimal Price { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public int AiItineraryGenerationsPerPeriod { get; set; }
    public int MaxPublishedPackages { get; set; }
    public string BenefitsText { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}
