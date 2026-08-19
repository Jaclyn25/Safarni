namespace Safarni.Domain.Entities;

public sealed class PackageBookingTraveler
{
    public Guid Id { get; set; }
    public Guid PackageBookingRequestId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public string Nationality { get; set; } = string.Empty;
    public string PassportNumber { get; set; } = string.Empty;
    public DateTime? PassportExpiry { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public PackageBookingRequest PackageBookingRequest { get; set; } = null!;
    public ICollection<PackageBookingDocument> Documents { get; set; } = new List<PackageBookingDocument>();
}
