namespace Safarni.Application.Abstractions.Security;

public interface IEntitlementService
{
    Task<UsageEntitlement> GetTravelerAiUsageAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UsageEntitlement> GetAgencyPackageUsageAsync(Guid agencyId, CancellationToken cancellationToken = default);
    Task<bool> HasTravelerPremiumAiAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<bool> HasAgencyMarketplacePaidAsync(
        Guid agencyId,
        CancellationToken cancellationToken = default);
}

public sealed record UsageEntitlement(
    bool IsAllowed,
    int Used,
    int Limit,
    DateTime? PeriodEnd,
    string? PlanCode);
