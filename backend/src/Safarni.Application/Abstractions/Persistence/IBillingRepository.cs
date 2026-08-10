using Safarni.Domain.Entities;
using Safarni.Domain.Enums;

namespace Safarni.Application.Abstractions.Persistence;

public interface IBillingRepository
{
    Task<IReadOnlyList<PricingPlan>> ListPlansAsync(
        PricingCustomerType? customerType,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PricingPlan>> ListAllPlansAsync(
        CancellationToken cancellationToken = default);

    Task<PricingPlan?> GetPlanAsync(Guid planId, CancellationToken cancellationToken = default);

    Task<PricingPlan?> GetPlanForAdminAsync(Guid planId, CancellationToken cancellationToken = default);

    Task<Subscription?> GetTravelerSubscriptionAsync(
        Guid userId,
        bool includePayments = false,
        CancellationToken cancellationToken = default);

    Task<Subscription?> GetAgencySubscriptionAsync(
        Guid agencyId,
        bool includePayments = false,
        CancellationToken cancellationToken = default);

    Task<PaymentTransaction?> GetPaymentBySessionAsync(
        string provider,
        string sessionId,
        CancellationToken cancellationToken = default);

    Task<Subscription?> GetSubscriptionByProviderReferenceAsync(
        string providerSubscriptionId,
        bool includePayments = false,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Subscription>> ListAllSubscriptionsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PaymentTransaction>> ListPaymentsAsync(
        Guid? agencyId = null,
        CancellationToken cancellationToken = default);

    Task<bool> HasActiveAgencySubscriptionAsync(
        Guid agencyId,
        DateTime now,
        CancellationToken cancellationToken = default);

    Task AddPlanAsync(PricingPlan plan, CancellationToken cancellationToken = default);
    Task AddSubscriptionAsync(Subscription subscription, CancellationToken cancellationToken = default);
    Task AddPaymentAsync(PaymentTransaction payment, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
