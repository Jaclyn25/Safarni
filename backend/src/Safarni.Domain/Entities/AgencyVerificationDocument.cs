using Safarni.Domain.Enums;

namespace Safarni.Domain.Entities;

public sealed class AgencyVerificationDocument
{
    public Guid Id { get; set; }

    public Guid TravelAgencyId { get; set; }

    public AgencyVerificationDocumentType DocumentType { get; set; }

    public string DocumentNumber { get; set; } = string.Empty;

    /// <summary>
    /// Private storage key or path. This value must not be exposed by public
    /// marketplace DTOs.
    /// </summary>
    public string StorageReference { get; set; } = string.Empty;

    public string OriginalFileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long FileSizeBytes { get; set; }

    public DateTime? ExpiryDate { get; set; }

    public DateTime UploadedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public AgencyDocumentReviewStatus ReviewStatus { get; set; } =
        AgencyDocumentReviewStatus.Pending;

    public string? ReviewReason { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public Guid? ReviewedByUserId { get; set; }

    public TravelAgency TravelAgency { get; set; } = null!;
}
