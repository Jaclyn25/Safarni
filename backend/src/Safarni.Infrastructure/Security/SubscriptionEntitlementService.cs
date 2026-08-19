using Microsoft.EntityFrameworkCore;
using Safarni.Application.Abstractions.Security;
using Safarni.Domain.Enums;
using Safarni.Infrastructure.Persistence;

namespace Safarni.Infrastructure.Security;

public sealed class SubscriptionEntitlementService : IEntitlementService
{
    private const int FreeTravelerAiGenerationsPerMonth = 3;
    private const int FreeAgencyPackageCreationsPerMonth = 3;
    private const int FreeAgencyPackagePublications = 1;
    private const string FreeTravelerPlanCode = "traveler-basic";
    private const string FreeAgencyPlanCode = "agency-basic";

    private readonly ApplicationDbContext _dbContext;

    public SubscriptionEntitlementService(ApplicationDbContext dbContext) => _dbContext = dbContext;

    public Task<bool> HasTravelerPremiumAiAsync(Guid userId, CancellationToken cancellationToken = default) =>
        HasActiveAsync(userId, null, PricingCustomerType.Traveler, cancellationToken);

    public Task<bool> HasAgencyMarketplacePaidAsync(Guid agencyId, CancellationToken cancellationToken = default) =>
        HasActiveAsync(null, agencyId, PricingCustomerType.Agency, cancellationToken);

    public async Task<UsageEntitlement> GetTravelerAiUsageAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var subscription = await ActiveSubscriptionAsync(userId, null, PricingCustomerType.Traveler, cancellationToken);
        var now = DateTime.UtcNow;

        if (subscription is null)
        {
            var freePeriodStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var freePeriodEnd = freePeriodStart.AddMonths(1);
            var freeUsed = await _dbContext.ItineraryGenerationJobs.CountAsync(job =>
                job.UserId == userId &&
                job.CreatedAt >= freePeriodStart &&
                job.CreatedAt < freePeriodEnd,
                cancellationToken);

            return new(
                freeUsed < FreeTravelerAiGenerationsPerMonth,
                freeUsed,
                FreeTravelerAiGenerationsPerMonth,
                freePeriodEnd,
                FreeTravelerPlanCode);
        }

        var periodStart = subscription.StartAt ?? subscription.CreatedAt;
        var used = await _dbContext.ItineraryGenerationJobs.CountAsync(job =>
            job.UserId == userId && job.CreatedAt >= periodStart && job.CreatedAt < subscription.CurrentPeriodEnd,
            cancellationToken);
        return new(used < subscription.Plan.AiItineraryGenerationsPerPeriod, used,
            subscription.Plan.AiItineraryGenerationsPerPeriod, subscription.CurrentPeriodEnd, subscription.Plan.Code);
    }

    public async Task<UsageEntitlement> GetAgencyPackageUsageAsync(Guid agencyId, CancellationToken cancellationToken = default)
    {
        var subscription = await ActiveSubscriptionAsync(null, agencyId, PricingCustomerType.Agency, cancellationToken);
        if (subscription is null)
        {
            var freeUsed = await _dbContext.TravelPackages.CountAsync(package =>
                package.TravelAgencyId == agencyId &&
                package.IsPublished &&
                package.IsActive,
                cancellationToken);
            return new(
                freeUsed < FreeAgencyPackagePublications,
                freeUsed,
                FreeAgencyPackagePublications,
                null,
                FreeAgencyPlanCode);
        }
        var used = await _dbContext.TravelPackages.CountAsync(package =>
            package.TravelAgencyId == agencyId && package.IsPublished && package.IsActive,
            cancellationToken);
        return new(used < subscription.Plan.MaxPublishedPackages, used,
            subscription.Plan.MaxPublishedPackages, subscription.CurrentPeriodEnd, subscription.Plan.Code);
    }

    public async Task<UsageEntitlement> GetAgencyPackageCreationUsageAsync(Guid agencyId, CancellationToken cancellationToken = default)
    {
        var subscription = await ActiveSubscriptionAsync(null, agencyId, PricingCustomerType.Agency, cancellationToken);
        if (subscription is not null)
        {
            return new(true, 0, int.MaxValue, subscription.CurrentPeriodEnd, subscription.Plan.Code);
        }

        var now = DateTime.UtcNow;
        var periodStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var periodEnd = periodStart.AddMonths(1);
        var freeUsed = await _dbContext.TravelPackages.CountAsync(package =>
            package.TravelAgencyId == agencyId &&
            package.CreatedAt >= periodStart &&
            package.CreatedAt < periodEnd,
            cancellationToken);
        return new(
            freeUsed < FreeAgencyPackageCreationsPerMonth,
            freeUsed,
            FreeAgencyPackageCreationsPerMonth,
            periodEnd,
            FreeAgencyPlanCode);
    }

    private async Task<bool> HasActiveAsync(Guid? userId, Guid? agencyId, PricingCustomerType type, CancellationToken cancellationToken)
        => await ActiveSubscriptionAsync(userId, agencyId, type, cancellationToken) is not null;

    private async Task<Domain.Entities.Subscription?> ActiveSubscriptionAsync(Guid? userId, Guid? agencyId, PricingCustomerType type, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var query = _dbContext.Subscriptions
            .AsNoTracking()
            .Where(subscription =>
                subscription.Status == SubscriptionStatus.Active &&
                subscription.Plan.CustomerType == type &&
                subscription.CurrentPeriodEnd > now);

        query = userId is { } travelerId
            ? query.Where(subscription => subscription.UserId == travelerId)
            : query.Where(subscription => subscription.TravelAgencyId == agencyId);

        return await query.OrderByDescending(subscription => subscription.CurrentPeriodEnd)
            .Include(subscription => subscription.Plan)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
