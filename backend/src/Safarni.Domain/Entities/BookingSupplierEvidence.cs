using Safarni.Domain.Enums;

namespace Safarni.Domain.Entities;

public sealed class BookingSupplierEvidence
{
    public Guid Id { get; set; }
    public Guid PackageBookingRequestId { get; set; }
    public Guid PackageBookingOfferId { get; set; }
    public Guid TravelAgencyId { get; set; }
    public Guid UploadedByUserId { get; set; }
    public string SupplierType { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string ConfirmationCode { get; set; } = string.Empty;
    public decimal InvoiceAmount { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public string StorageReference { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public long SizeBytes { get; set; }
    public BookingSupplierEvidenceStatus Status { get; set; } = BookingSupplierEvidenceStatus.PendingReview;
    public Guid? ReviewedByUserId { get; set; }
    public string? ReviewNote { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }

    public PackageBookingRequest PackageBookingRequest { get; set; } = null!;
    public PackageBookingOffer PackageBookingOffer { get; set; } = null!;
    public TravelAgency TravelAgency { get; set; } = null!;
}
