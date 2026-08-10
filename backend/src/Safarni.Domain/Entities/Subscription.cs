using Safarni.Domain.Enums;

namespace Safarni.Domain.Entities;

public sealed class Subscription
{
    public Guid Id { get; set; }
    public Guid PlanId { get; set; }
    public Guid? UserId { get; set; }
    public Guid? TravelAgencyId { get; set; }
    public string? ProviderSubscriptionId { get; set; }
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Pending;
    public DateTime? StartAt { get; set; }
    public DateTime? CurrentPeriodEnd { get; set; }
    public bool CancelAtPeriodEnd { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public PricingPlan Plan { get; set; } = null!;
    public User? User { get; set; }
    public TravelAgency? TravelAgency { get; set; }
    public ICollection<PaymentTransaction> Payments { get; set; } = new List<PaymentTransaction>();
}
