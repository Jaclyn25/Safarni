using Safarni.Domain.Enums;

namespace Safarni.Domain.Entities;

public sealed class PackageBookingOffer
{
    public Guid Id { get; set; }
    public Guid PackageBookingRequestId { get; set; }
    public int Version { get; set; }
    public decimal Amount { get; set; }
    public decimal SupplierCostAmount { get; set; }
    public decimal AgencyServiceFeeAmount { get; set; }
    public decimal SafarniProtectionFeeAmount { get; set; }
    public decimal InitialPaymentPercentage { get; set; } = 30m;
    public DateTime? FinalPaymentDueAt { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public int TravelerCount { get; set; }
    public string? Included { get; set; }
    public string? Excluded { get; set; }
    public string? CancellationPolicy { get; set; }
    public string? AgencyNote { get; set; }
    public PackageBookingOfferStatus Status { get; set; } = PackageBookingOfferStatus.Sent;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime? SupersededAt { get; set; }

    public PackageBookingRequest PackageBookingRequest { get; set; } = null!;
}
