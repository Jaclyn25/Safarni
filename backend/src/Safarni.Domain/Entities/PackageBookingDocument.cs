namespace Safarni.Domain.Entities;

public sealed class PackageBookingDocument
{
    public Guid Id { get; set; }
    public Guid PackageBookingRequestId { get; set; }
    public Guid? PackageBookingTravelerId { get; set; }
    public Guid UploadedByUserId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string StorageReference { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public long SizeBytes { get; set; }
    public DateTime CreatedAt { get; set; }

    public PackageBookingRequest PackageBookingRequest { get; set; } = null!;
    public PackageBookingTraveler? PackageBookingTraveler { get; set; }
}
