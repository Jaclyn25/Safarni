using Safarni.Domain.Enums;

namespace Safarni.Domain.Entities;

public sealed class PaymentTransaction
{
    public Guid Id { get; set; }
    public Guid SubscriptionId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string PaymentSessionId { get; set; } = string.Empty;
    public string? ProviderEventId { get; set; }
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public Subscription Subscription { get; set; } = null!;
}
