using MediatR;
using Safarni.Application.Abstractions.Authentication;
using Safarni.Application.Abstractions.Payments;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Abstractions.Security;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;
using Safarni.Domain.Entities;
using Safarni.Domain.Enums;

namespace Safarni.Application.Features.Billing;

public sealed record PricingPlanResponse(
    Guid Id,
    string Code,
    string DisplayName,
    string Description,
    PricingCustomerType CustomerType,
    BillingPeriod BillingPeriod,
    decimal Price,
    string CurrencyCode,
    int AiItineraryGenerationsPerPeriod,
    int MaxPublishedPackages,
    IReadOnlyList<string> Benefits,
    bool IsActive);

public sealed record SubscriptionResponse(
    Guid Id,
    PricingPlanResponse Plan,
    SubscriptionStatus Status,
    DateTime? StartAt,
    DateTime? CurrentPeriodEnd,
    bool CancelAtPeriodEnd,
    string? PaymentSessionId);

public sealed record CheckoutResponse(
    Guid SubscriptionId,
    string Provider,
    string PaymentSessionId,
    string CheckoutUrl,
    SubscriptionResponse Subscription);

public sealed record GetPricingPlansQuery(PricingCustomerType? CustomerType)
    : IRequest<Result<IReadOnlyList<PricingPlanResponse>>>;

public sealed record GetMySubscriptionQuery(Guid UserId)
    : IRequest<Result<SubscriptionResponse>>;

public sealed record CreateCheckoutSessionCommand(Guid UserId, Guid PlanId)
    : IRequest<Result<CheckoutResponse>>;

public sealed record CancelMySubscriptionCommand(Guid UserId)
    : IRequest<Result<SubscriptionResponse>>;

public sealed record ProcessPaymentWebhookCommand(PaymentWebhookEvent Event)
    : IRequest<Result<SubscriptionResponse>>;

internal static class BillingMapping
{
    public static PricingPlanResponse ToPlanResponse(PricingPlan plan) => new(
        plan.Id, plan.Code, plan.DisplayName, plan.Description, plan.CustomerType,
        plan.BillingPeriod, plan.Price, plan.CurrencyCode,
        plan.AiItineraryGenerationsPerPeriod, plan.MaxPublishedPackages,
        plan.BenefitsText.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
        plan.IsActive);

    public static SubscriptionResponse ToSubscriptionResponse(Subscription subscription) => new(
        subscription.Id,
        ToPlanResponse(subscription.Plan),
        subscription.Status,
        subscription.StartAt,
        subscription.CurrentPeriodEnd,
        subscription.CancelAtPeriodEnd,
        subscription.Payments.OrderByDescending(payment => payment.CreatedAt)
            .Select(payment => payment.PaymentSessionId).FirstOrDefault());

    public static void RefreshLifecycle(Subscription subscription, DateTime now)
    {
        if (subscription.Status is SubscriptionStatus.Active or SubscriptionStatus.PastDue &&
            subscription.CurrentPeriodEnd is { } periodEnd && periodEnd <= now)
        {
            subscription.Status = SubscriptionStatus.Expired;
            subscription.UpdatedAt = now;
        }
    }
}

public sealed class GetPricingPlansQueryHandler : IRequestHandler<GetPricingPlansQuery, Result<IReadOnlyList<PricingPlanResponse>>>
{
    private readonly IBillingRepository _billing;

    public GetPricingPlansQueryHandler(IBillingRepository billing) => _billing = billing;

    public async Task<Result<IReadOnlyList<PricingPlanResponse>>> Handle(GetPricingPlansQuery request, CancellationToken cancellationToken)
    {
        var plans = await _billing.ListPlansAsync(request.CustomerType, cancellationToken);
        return Result<IReadOnlyList<PricingPlanResponse>>.Success(plans.Select(BillingMapping.ToPlanResponse).ToArray());
    }
}

public sealed class GetMySubscriptionQueryHandler : IRequestHandler<GetMySubscriptionQuery, Result<SubscriptionResponse>>
{
    private readonly IUserRepository _users;
    private readonly ITravelAgencyRepository _agencies;
    private readonly IBillingRepository _billing;

    public GetMySubscriptionQueryHandler(IUserRepository users, ITravelAgencyRepository agencies, IBillingRepository billing)
    {
        _users = users; _agencies = agencies; _billing = billing;
    }

    public async Task<Result<SubscriptionResponse>> Handle(GetMySubscriptionQuery request, CancellationToken cancellationToken)
    {
        var account = await BillingAccount.ResolveAsync(request.UserId, _users, _agencies, cancellationToken);
        if (account is null) return Result<SubscriptionResponse>.Failure(BillingErrors.BillingAccountRequired);
        var subscription = account.CustomerType == PricingCustomerType.Traveler
            ? await _billing.GetTravelerSubscriptionAsync(request.UserId, includePayments: true, cancellationToken)
            : await _billing.GetAgencySubscriptionAsync(account.AgencyId!.Value, includePayments: true, cancellationToken);
        if (subscription is null) return Result<SubscriptionResponse>.Failure(BillingErrors.SubscriptionNotFound);
        BillingMapping.RefreshLifecycle(subscription, DateTime.UtcNow);
        if (subscription.UpdatedAt is not null) await _billing.SaveChangesAsync(cancellationToken);
        return Result<SubscriptionResponse>.Success(BillingMapping.ToSubscriptionResponse(subscription));
    }
}

public sealed class CreateCheckoutSessionCommandHandler : IRequestHandler<CreateCheckoutSessionCommand, Result<CheckoutResponse>>
{
    private readonly IUserRepository _users;
    private readonly ITravelAgencyRepository _agencies;
    private readonly IBillingRepository _billing;
    private readonly IPaymentGateway _gateway;

    public CreateCheckoutSessionCommandHandler(IUserRepository users, ITravelAgencyRepository agencies, IBillingRepository billing, IPaymentGateway gateway)
    {
        _users = users; _agencies = agencies; _billing = billing; _gateway = gateway;
    }

    public async Task<Result<CheckoutResponse>> Handle(CreateCheckoutSessionCommand request, CancellationToken cancellationToken)
    {
        var account = await BillingAccount.ResolveAsync(request.UserId, _users, _agencies, cancellationToken);
        if (account is null) return Result<CheckoutResponse>.Failure(BillingErrors.BillingAccountRequired);
        var plan = await _billing.GetPlanAsync(request.PlanId, cancellationToken);
        if (plan is null) return Result<CheckoutResponse>.Failure(BillingErrors.PlanNotFound);
        if (plan.CustomerType != account.CustomerType) return Result<CheckoutResponse>.Failure(BillingErrors.PlanCustomerTypeMismatch);

        var existing = account.CustomerType == PricingCustomerType.Traveler
            ? await _billing.GetTravelerSubscriptionAsync(request.UserId, cancellationToken: cancellationToken)
            : await _billing.GetAgencySubscriptionAsync(account.AgencyId!.Value, cancellationToken: cancellationToken);
        if (existing is not null)
        {
            BillingMapping.RefreshLifecycle(existing, DateTime.UtcNow);
            if (existing.Status is SubscriptionStatus.Active or SubscriptionStatus.Pending)
                return Result<CheckoutResponse>.Failure(BillingErrors.DuplicateActiveSubscription);
        }

        var now = DateTime.UtcNow;
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(), PlanId = plan.Id, Plan = plan,
            UserId = account.CustomerType == PricingCustomerType.Traveler ? request.UserId : null,
            TravelAgencyId = account.AgencyId,
            Status = SubscriptionStatus.Pending, CreatedAt = now
        };
        await _billing.AddSubscriptionAsync(subscription, cancellationToken);
        PaymentCheckoutSession checkout;
        try
        {
            checkout = await _gateway.CreateCheckoutSessionAsync(new PaymentCheckoutRequest(
                request.UserId.ToString(), plan.Price, plan.CurrencyCode, plan.DisplayName,
                new Dictionary<string, string> { ["subscriptionId"] = subscription.Id.ToString(), ["customerType"] = plan.CustomerType.ToString() },
                plan.BillingPeriod,
                account.CustomerEmail), cancellationToken);
        }
        catch (InvalidOperationException exception) when (exception.Message.Contains("not configured", StringComparison.OrdinalIgnoreCase))
        {
            return Result<CheckoutResponse>.Failure(BillingErrors.PaymentProviderNotConfigured);
        }
        var payment = new PaymentTransaction
        {
            Id = Guid.NewGuid(), SubscriptionId = subscription.Id, Subscription = subscription,
            Provider = checkout.Provider, PaymentSessionId = checkout.SessionId,
            Amount = plan.Price, CurrencyCode = plan.CurrencyCode, Status = PaymentStatus.Pending, CreatedAt = now
        };
        subscription.Payments.Add(payment);
        await _billing.AddPaymentAsync(payment, cancellationToken);
        await _billing.SaveChangesAsync(cancellationToken);
        return Result<CheckoutResponse>.Success(new CheckoutResponse(
            subscription.Id, checkout.Provider, checkout.SessionId, checkout.CheckoutUrl,
            BillingMapping.ToSubscriptionResponse(subscription)));
    }
}

public sealed class CancelMySubscriptionCommandHandler : IRequestHandler<CancelMySubscriptionCommand, Result<SubscriptionResponse>>
{
    private readonly IUserRepository _users;
    private readonly ITravelAgencyRepository _agencies;
    private readonly IBillingRepository _billing;
    private readonly IPaymentGateway _gateway;

    public CancelMySubscriptionCommandHandler(
        IUserRepository users,
        ITravelAgencyRepository agencies,
        IBillingRepository billing,
        IPaymentGateway gateway)
    {
        _users = users; _agencies = agencies; _billing = billing; _gateway = gateway;
    }

    public async Task<Result<SubscriptionResponse>> Handle(CancelMySubscriptionCommand request, CancellationToken cancellationToken)
    {
        var account = await BillingAccount.ResolveAsync(request.UserId, _users, _agencies, cancellationToken);
        if (account is null) return Result<SubscriptionResponse>.Failure(BillingErrors.BillingAccountRequired);
        var subscription = account.CustomerType == PricingCustomerType.Traveler
            ? await _billing.GetTravelerSubscriptionAsync(request.UserId, includePayments: true, cancellationToken)
            : await _billing.GetAgencySubscriptionAsync(account.AgencyId!.Value, includePayments: true, cancellationToken);
        if (subscription is null) return Result<SubscriptionResponse>.Failure(BillingErrors.SubscriptionNotFound);
        BillingMapping.RefreshLifecycle(subscription, DateTime.UtcNow);
        if (subscription.Status != SubscriptionStatus.Active) return Result<SubscriptionResponse>.Failure(BillingErrors.SubscriptionCancellationNotAllowed);
        if (!string.IsNullOrWhiteSpace(subscription.ProviderSubscriptionId))
        {
            await _gateway.ScheduleSubscriptionCancellationAsync(subscription.ProviderSubscriptionId, cancellationToken);
        }
        subscription.CancelAtPeriodEnd = true;
        subscription.UpdatedAt = DateTime.UtcNow;
        await _billing.SaveChangesAsync(cancellationToken);
        return Result<SubscriptionResponse>.Success(BillingMapping.ToSubscriptionResponse(subscription));
    }
}

public sealed class ProcessPaymentWebhookCommandHandler : IRequestHandler<ProcessPaymentWebhookCommand, Result<SubscriptionResponse>>
{
    private readonly IBillingRepository _billing;

    public ProcessPaymentWebhookCommandHandler(IBillingRepository billing) => _billing = billing;

    public async Task<Result<SubscriptionResponse>> Handle(ProcessPaymentWebhookCommand request, CancellationToken cancellationToken)
    {
        var payment = await _billing.GetPaymentBySessionAsync(request.Event.Provider, request.Event.SessionId, cancellationToken);
        var subscription = payment?.Subscription;
        if (subscription is null && !string.IsNullOrWhiteSpace(request.Event.ProviderSubscriptionId))
        {
            subscription = await _billing.GetSubscriptionByProviderReferenceAsync(
                request.Event.ProviderSubscriptionId,
                includePayments: true,
                cancellationToken);
        }

        if (subscription is null) return Result<SubscriptionResponse>.Failure(BillingErrors.PaymentSessionNotFound);
        if (payment?.ProviderEventId == request.Event.EventId)
            return Result<SubscriptionResponse>.Success(BillingMapping.ToSubscriptionResponse(subscription));

        if (payment is null)
        {
            payment = new PaymentTransaction
            {
                Id = Guid.NewGuid(),
                SubscriptionId = subscription.Id,
                Subscription = subscription,
                Provider = request.Event.Provider,
                PaymentSessionId = request.Event.SessionId,
                Amount = request.Event.Amount ?? subscription.Plan.Price,
                CurrencyCode = (request.Event.CurrencyCode ?? subscription.Plan.CurrencyCode).ToUpperInvariant(),
                Status = PaymentStatus.Pending,
                CreatedAt = request.Event.OccurredAt
            };
            subscription.Payments.Add(payment);
            await _billing.AddPaymentAsync(payment, cancellationToken);
        }

        var now = DateTime.UtcNow;
        payment.ProviderEventId = request.Event.EventId;
        payment.ProcessedAt = now;
        payment.Status = request.Event.Status;
        payment.FailureReason = request.Event.FailureReason;
        payment.Subscription.Status = request.Event.Status == PaymentStatus.Succeeded
            ? SubscriptionStatus.Active
            : SubscriptionStatus.PaymentFailed;
        payment.Subscription.ProviderSubscriptionId ??= request.Event.ProviderSubscriptionId;
        if (request.Event.Status == PaymentStatus.Succeeded)
        {
            payment.Subscription.StartAt ??= now;
            payment.Subscription.CurrentPeriodEnd = request.Event.CurrentPeriodEnd
                ?? (payment.Subscription.Plan.BillingPeriod == BillingPeriod.Monthly
                    ? now.AddMonths(1)
                    : now.AddYears(1));
            payment.Subscription.CancelAtPeriodEnd = false;
        }
        payment.Subscription.UpdatedAt = now;
        await _billing.SaveChangesAsync(cancellationToken);
        return Result<SubscriptionResponse>.Success(BillingMapping.ToSubscriptionResponse(payment.Subscription));
    }
}

internal sealed record BillingAccount(PricingCustomerType CustomerType, Guid? AgencyId, string? CustomerEmail)
{
    public static async Task<BillingAccount?> ResolveAsync(Guid userId, IUserRepository users, ITravelAgencyRepository agencies, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(userId, cancellationToken);
        return user?.Role switch
        {
            UserRole.User => new BillingAccount(PricingCustomerType.Traveler, null, user.Email),
            UserRole.TravelAgency when await agencies.GetByUserIdAsync(userId, cancellationToken: cancellationToken) is { } agency => new BillingAccount(PricingCustomerType.Agency, agency.Id, user.Email),
            _ => null
        };
    }
}
