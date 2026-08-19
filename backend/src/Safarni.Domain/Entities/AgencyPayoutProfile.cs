using Safarni.Domain.Enums;

namespace Safarni.Domain.Entities;

public sealed class AgencyPayoutProfile
{
    public Guid Id { get; set; }
    public Guid TravelAgencyId { get; set; }
    public AgencyPayoutMethod Method { get; set; }
    public AgencyPayoutProfileStatus Status { get; set; } = AgencyPayoutProfileStatus.PendingReview;
    public string AccountHolderName { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string? BankName { get; set; }
    public string? Iban { get; set; }
    public string? AccountNumber { get; set; }
    public string? SwiftCode { get; set; }
    public string? WalletProvider { get; set; }
    public string? WalletNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? VerifiedAt { get; set; }

    public TravelAgency TravelAgency { get; set; } = null!;
}
