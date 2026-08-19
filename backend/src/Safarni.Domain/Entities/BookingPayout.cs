using Safarni.Domain.Enums;

namespace Safarni.Domain.Entities;

public sealed class BookingPayout
{
    public Guid Id { get; set; }
    public Guid PackageBookingRequestId { get; set; }
    public Guid PackageBookingOfferId { get; set; }
    public Guid TravelAgencyId { get; set; }
    public Guid? SupplierEvidenceId { get; set; }
    public BookingPayoutType Type { get; set; }
    public BookingPayoutStatus Status { get; set; } = BookingPayoutStatus.PendingReview;
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public string? ExternalTransferId { get; set; }
    public string? ReviewNote { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? ReleaseAt { get; set; }

    public PackageBookingRequest PackageBookingRequest { get; set; } = null!;
    public PackageBookingOffer PackageBookingOffer { get; set; } = null!;
    public TravelAgency TravelAgency { get; set; } = null!;
    public BookingSupplierEvidence? SupplierEvidence { get; set; }
}
