using MediatR;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;
using Safarni.Application.Features.Billing;
using Safarni.Domain.Entities;
using Safarni.Domain.Enums;

namespace Safarni.Application.Features.Admin.Billing;

public sealed record AdminBillingRevenueResponse(
    string CurrencyCode,
    decimal GrossRevenue,
    int SuccessfulPayments);

public sealed record AdminBillingPlanResponse(
    Guid Id,
    string Code,
    string DisplayName,
    PricingCustomerType CustomerType,
    BillingPeriod BillingPeriod,
    decimal Price,
    string CurrencyCode,
    decimal BookingCommissionRate,
    int AiItineraryGenerationsPerPeriod,
    int MaxPublishedPackages,
    bool IsActive,
    int TotalSubscriptions,
    int ActiveSubscriptions,
    decimal GrossRevenue,
    int SuccessfulPayments);

public sealed record AdminBillingPaymentResponse(
    Guid Id,
    string PlanDisplayName,
    PricingCustomerType CustomerType,
    decimal Amount,
    string CurrencyCode,
    PaymentStatus Status,
    string CustomerName,
    DateTime CreatedAt);

public sealed record AdminBillingOverviewResponse(
    int TotalPlans,
    int ActivePlans,
    int TotalSubscriptions,
    int ActiveSubscriptions,
    int PendingSubscriptions,
    int FailedSubscriptions,
    int CancelledSubscriptions,
    int ExpiredSubscriptions,
    int TotalPayments,
    int SuccessfulPayments,
    int FailedPayments,
    IReadOnlyList<AdminBillingRevenueResponse> RevenueByCurrency,
    IReadOnlyList<AdminBillingPlanResponse> Plans,
    IReadOnlyList<AdminBillingPaymentResponse> RecentPayments);

public sealed record GetAdminBillingOverviewQuery()
    : IRequest<Result<AdminBillingOverviewResponse>>;

public sealed record UpdatePricingPlanCommand(
    Guid PlanId,
    decimal Price,
    string CurrencyCode,
    int AiItineraryGenerationsPerPeriod,
    int MaxPublishedPackages,
    decimal? BookingCommissionRate,
    bool IsActive)
    : IRequest<Result<PricingPlanResponse>>;

public sealed class GetAdminBillingOverviewQueryHandler
    : IRequestHandler<GetAdminBillingOverviewQuery, Result<AdminBillingOverviewResponse>>
{
    private readonly IBillingRepository _billing;

    public GetAdminBillingOverviewQueryHandler(IBillingRepository billing) => _billing = billing;

    public async Task<Result<AdminBillingOverviewResponse>> Handle(
        GetAdminBillingOverviewQuery request,
        CancellationToken cancellationToken)
    {
        var plans = await _billing.ListAllPlansAsync(cancellationToken);
        var subscriptions = await _billing.ListAllSubscriptionsAsync(cancellationToken);
        var payments = await _billing.ListPaymentsAsync(cancellationToken: cancellationToken);

        var successfulPayments = payments.Where(payment => payment.Status == PaymentStatus.Succeeded).ToArray();
        var revenue = successfulPayments
            .GroupBy(payment => payment.CurrencyCode)
            .Select(group => new AdminBillingRevenueResponse(
                group.Key,
                group.Sum(payment => payment.Amount),
                group.Count()))
            .OrderBy(item => item.CurrencyCode)
            .ToArray();

        var planSummaries = plans.Select(plan =>
        {
            var planSubscriptions = subscriptions.Where(subscription => subscription.PlanId == plan.Id).ToArray();
            var planPayments = successfulPayments.Where(payment => payment.Subscription.PlanId == plan.Id).ToArray();
            return new AdminBillingPlanResponse(
                plan.Id,
                plan.Code,
                plan.DisplayName,
                plan.CustomerType,
                plan.BillingPeriod,
                plan.Price,
                plan.CurrencyCode,
                plan.BookingCommissionRate,
                plan.AiItineraryGenerationsPerPeriod,
                plan.MaxPublishedPackages,
                plan.IsActive,
                planSubscriptions.Length,
                planSubscriptions.Count(subscription => subscription.Status == SubscriptionStatus.Active),
                planPayments.Sum(payment => payment.Amount),
                planPayments.Length);
        }).ToArray();

        var recentPayments = payments
            .Take(20)
            .Select(payment => new AdminBillingPaymentResponse(
                payment.Id,
                payment.Subscription.Plan.DisplayName,
                payment.Subscription.Plan.CustomerType,
                payment.Amount,
                payment.CurrencyCode,
                payment.Status,
                payment.Subscription.TravelAgency?.CompanyName
                    ?? payment.Subscription.User?.FullName
                    ?? "Unknown customer",
                payment.CreatedAt))
            .ToArray();

        return Result<AdminBillingOverviewResponse>.Success(new AdminBillingOverviewResponse(
            plans.Count,
            plans.Count(plan => plan.IsActive),
            subscriptions.Count,
            subscriptions.Count(subscription => subscription.Status == SubscriptionStatus.Active),
            subscriptions.Count(subscription => subscription.Status == SubscriptionStatus.Pending),
            subscriptions.Count(subscription => subscription.Status == SubscriptionStatus.PaymentFailed || subscription.Status == SubscriptionStatus.PastDue),
            subscriptions.Count(subscription => subscription.Status == SubscriptionStatus.Cancelled),
            subscriptions.Count(subscription => subscription.Status == SubscriptionStatus.Expired),
            payments.Count,
            successfulPayments.Length,
            payments.Count(payment => payment.Status == PaymentStatus.Failed),
            revenue,
            planSummaries,
            recentPayments));
    }
}

public sealed class UpdatePricingPlanCommandHandler
    : IRequestHandler<UpdatePricingPlanCommand, Result<PricingPlanResponse>>
{
    private readonly IBillingRepository _billing;

    public UpdatePricingPlanCommandHandler(IBillingRepository billing) => _billing = billing;

    public async Task<Result<PricingPlanResponse>> Handle(
        UpdatePricingPlanCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Price < 0)
        {
            return Result<PricingPlanResponse>.Failure(BillingErrors.InvalidPlanPrice);
        }

        var currencyCode = request.CurrencyCode.Trim().ToUpperInvariant();
        if (currencyCode.Length != 3 || currencyCode.Any(character => character is < 'A' or > 'Z'))
        {
            return Result<PricingPlanResponse>.Failure(BillingErrors.InvalidCurrencyCode);
        }

        if (request.AiItineraryGenerationsPerPeriod < 0 || request.MaxPublishedPackages < 0 ||
            request.BookingCommissionRate is < 0m or > 100m)
        {
            return Result<PricingPlanResponse>.Failure(BillingErrors.InvalidPlanLimits);
        }

        var plan = await _billing.GetPlanForAdminAsync(request.PlanId, cancellationToken);
        if (plan is null)
        {
            return Result<PricingPlanResponse>.Failure(BillingErrors.PlanNotFound);
        }

        plan.Price = decimal.Round(request.Price, 2, MidpointRounding.AwayFromZero);
        plan.CurrencyCode = currencyCode;
        plan.AiItineraryGenerationsPerPeriod = request.AiItineraryGenerationsPerPeriod;
        plan.MaxPublishedPackages = request.MaxPublishedPackages;
        if (request.BookingCommissionRate is { } bookingCommissionRate)
        {
            plan.BookingCommissionRate = decimal.Round(bookingCommissionRate, 2, MidpointRounding.AwayFromZero);
        }
        plan.IsActive = request.IsActive;
        await _billing.SaveChangesAsync(cancellationToken);

        return Result<PricingPlanResponse>.Success(BillingMapping.ToPlanResponse(plan));
    }
}
