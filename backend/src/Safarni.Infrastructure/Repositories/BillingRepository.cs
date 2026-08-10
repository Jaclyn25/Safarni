using Microsoft.EntityFrameworkCore;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Domain.Entities;
using Safarni.Domain.Enums;
using Safarni.Infrastructure.Persistence;

namespace Safarni.Infrastructure.Repositories;

public sealed class BillingRepository : IBillingRepository
{
    private readonly ApplicationDbContext _dbContext;

    public BillingRepository(ApplicationDbContext dbContext) => _dbContext = dbContext;

    public async Task<IReadOnlyList<PricingPlan>> ListPlansAsync(
        PricingCustomerType? customerType,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.PricingPlans.AsNoTracking().Where(plan => plan.IsActive);
        if (customerType is not null) query = query.Where(plan => plan.CustomerType == customerType);
        return await query.OrderBy(plan => plan.CustomerType).ThenBy(plan => plan.BillingPeriod).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PricingPlan>> ListAllPlansAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.PricingPlans
            .AsNoTracking()
            .OrderBy(plan => plan.CustomerType)
            .ThenBy(plan => plan.BillingPeriod)
            .ToListAsync(cancellationToken);
    }

    public Task<PricingPlan?> GetPlanAsync(Guid planId, CancellationToken cancellationToken = default) =>
        _dbContext.PricingPlans.FirstOrDefaultAsync(plan => plan.Id == planId && plan.IsActive, cancellationToken);

    public Task<PricingPlan?> GetPlanForAdminAsync(Guid planId, CancellationToken cancellationToken = default) =>
        _dbContext.PricingPlans.FirstOrDefaultAsync(plan => plan.Id == planId, cancellationToken);

    public Task<Subscription?> GetTravelerSubscriptionAsync(Guid userId, bool includePayments = false, CancellationToken cancellationToken = default) =>
        SubscriptionQuery(includePayments).Where(subscription => subscription.UserId == userId).OrderByDescending(subscription => subscription.CreatedAt).FirstOrDefaultAsync(cancellationToken);

    public Task<Subscription?> GetAgencySubscriptionAsync(Guid agencyId, bool includePayments = false, CancellationToken cancellationToken = default) =>
        SubscriptionQuery(includePayments).Where(subscription => subscription.TravelAgencyId == agencyId).OrderByDescending(subscription => subscription.CreatedAt).FirstOrDefaultAsync(cancellationToken);

    public Task<PaymentTransaction?> GetPaymentBySessionAsync(string provider, string sessionId, CancellationToken cancellationToken = default) =>
        _dbContext.PaymentTransactions
            .Include(payment => payment.Subscription).ThenInclude(subscription => subscription.Plan)
            .Include(payment => payment.Subscription).ThenInclude(subscription => subscription.TravelAgency)
            .FirstOrDefaultAsync(payment => payment.Provider == provider && payment.PaymentSessionId == sessionId, cancellationToken);

    public Task<Subscription?> GetSubscriptionByProviderReferenceAsync(
        string providerSubscriptionId,
        bool includePayments = false,
        CancellationToken cancellationToken = default) =>
        SubscriptionQuery(includePayments)
            .Where(subscription => subscription.ProviderSubscriptionId == providerSubscriptionId)
            .OrderByDescending(subscription => subscription.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<Subscription>> ListAllSubscriptionsAsync(
        CancellationToken cancellationToken = default)
    {
        return await SubscriptionQuery(includePayments: false)
            .AsNoTracking()
            .OrderByDescending(subscription => subscription.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PaymentTransaction>> ListPaymentsAsync(
        Guid? agencyId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.PaymentTransactions
            .AsNoTracking()
            .Include(payment => payment.Subscription).ThenInclude(subscription => subscription.Plan)
            .Include(payment => payment.Subscription).ThenInclude(subscription => subscription.TravelAgency)
            .Include(payment => payment.Subscription).ThenInclude(subscription => subscription.User)
            .AsQueryable();

        if (agencyId is not null)
        {
            query = query.Where(payment => payment.Subscription.TravelAgencyId == agencyId);
        }

        return await query
            .OrderByDescending(payment => payment.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> HasActiveAgencySubscriptionAsync(Guid agencyId, DateTime now, CancellationToken cancellationToken = default) =>
        _dbContext.Subscriptions.AnyAsync(subscription =>
            subscription.TravelAgencyId == agencyId &&
            subscription.Status == SubscriptionStatus.Active &&
            subscription.CurrentPeriodEnd > now,
            cancellationToken);

    public Task AddPlanAsync(PricingPlan plan, CancellationToken cancellationToken = default) => _dbContext.PricingPlans.AddAsync(plan, cancellationToken).AsTask();
    public Task AddSubscriptionAsync(Subscription subscription, CancellationToken cancellationToken = default) => _dbContext.Subscriptions.AddAsync(subscription, cancellationToken).AsTask();
    public Task AddPaymentAsync(PaymentTransaction payment, CancellationToken cancellationToken = default) => _dbContext.PaymentTransactions.AddAsync(payment, cancellationToken).AsTask();
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => _dbContext.SaveChangesAsync(cancellationToken);

    private IQueryable<Subscription> SubscriptionQuery(bool includePayments)
    {
        var query = _dbContext.Subscriptions
            .Include(subscription => subscription.Plan)
            .Include(subscription => subscription.User)
            .Include(subscription => subscription.TravelAgency);
        return includePayments ? query.Include(subscription => subscription.Payments) : query;
    }
}
