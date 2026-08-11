using Safarni.Domain.Enums;

namespace Safarni.Domain.Entities;

public sealed class TravelAgency
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string CompanyName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;

    public string CompanyPhone { get; set; } = string.Empty;

    public string? LogoUrl { get; set; }

    public string? WebsiteUrl { get; set; }

    public AgencyVerificationStatus VerificationStatus { get; set; } =
        AgencyVerificationStatus.Pending;

    public string? VerificationDecisionReason { get; set; }

    public DateTime? VerificationReviewedAt { get; set; }

    public Guid? VerificationReviewedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public User User { get; set; } = null!;

    public ICollection<AgencyVerificationDocument> VerificationDocuments { get; set; } =
        new List<AgencyVerificationDocument>();

    public ICollection<TravelPackage> Packages { get; set; } = new List<TravelPackage>();

    public ICollection<AgencyReview> Reviews { get; set; } = new List<AgencyReview>();

    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}
