using Safarni.Domain.Enums;

namespace Safarni.Domain.Entities;

public sealed class PackageBookingRequest
{
    public Guid Id { get; set; }
    public Guid TravelPackageId { get; set; }
    public Guid TravelerUserId { get; set; }
    public int TravelerCount { get; set; } = 1;
    public string? Message { get; set; }
    public PackageBookingRequestStatus Status { get; set; } = PackageBookingRequestStatus.Pending;
    public string? AgencyResponse { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
    public TravelPackage TravelPackage { get; set; } = null!;
    public User TravelerUser { get; set; } = null!;
}
