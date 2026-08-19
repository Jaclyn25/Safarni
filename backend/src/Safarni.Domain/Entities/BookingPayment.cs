using Safarni.Domain.Enums;

namespace Safarni.Domain.Entities;

public sealed class BookingPayment
{
    public Guid Id { get; set; }
    public Guid PackageBookingRequestId { get; set; }
    public Guid PackageBookingOfferId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string PaymentSessionId { get; set; } = string.Empty;
    public string? ProviderEventId { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal CommissionRate { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal AgencyAmount { get; set; }
    public int InstallmentNumber { get; set; } = 1;
    public DateTime? DueAt { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public BookingPaymentStatus Status { get; set; } = BookingPaymentStatus.Pending;
    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? RefundedAt { get; set; }
    public decimal? RefundedAmount { get; set; }
    public decimal? RefundPercentage { get; set; }
    public string? ProviderRefundId { get; set; }

    public PackageBookingRequest PackageBookingRequest { get; set; } = null!;
    public PackageBookingOffer PackageBookingOffer { get; set; } = null!;
}
