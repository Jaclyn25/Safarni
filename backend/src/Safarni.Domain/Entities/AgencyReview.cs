namespace Safarni.Domain.Entities;

public sealed class AgencyReview
{
    public Guid Id { get; set; }
    public Guid TravelAgencyId { get; set; }
    public Guid TravelerUserId { get; set; }
    public Guid PackageBookingRequestId { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public string? AgencyResponse { get; set; }
    public bool IsPublished { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? AgencyRespondedAt { get; set; }

    public TravelAgency TravelAgency { get; set; } = null!;
    public User TravelerUser { get; set; } = null!;
    public PackageBookingRequest PackageBookingRequest { get; set; } = null!;
}
