namespace Safarni.Application.Common.Errors;

public static class BillingErrors
{
    public static readonly Error PlanNotFound = new(
        "Billing.PlanNotFound",
        "The selected pricing plan is not available.",
        ErrorType.NotFound);

    public static readonly Error PlanCustomerTypeMismatch = new(
        "Billing.PlanCustomerTypeMismatch",
        "This plan is not available for your account type.",
        ErrorType.Forbidden);

    public static readonly Error BillingAccountRequired = new(
        "Billing.BillingAccountRequired",
        "This account cannot purchase a Safarni subscription.",
        ErrorType.Forbidden);

    public static readonly Error DuplicateActiveSubscription = new(
        "Billing.DuplicateActiveSubscription",
        "Your account already has an active or pending subscription.",
        ErrorType.Conflict);

    public static readonly Error SubscriptionNotFound = new(
        "Billing.SubscriptionNotFound",
        "No subscription was found for this account.",
        ErrorType.NotFound);

    public static readonly Error SubscriptionCancellationNotAllowed = new(
        "Billing.SubscriptionCancellationNotAllowed",
        "Only an active subscription can be cancelled.",
        ErrorType.Validation);

    public static readonly Error PaymentSessionNotFound = new(
        "Billing.PaymentSessionNotFound",
        "The payment session was not found.",
        ErrorType.NotFound);

    public static readonly Error PaymentWebhookInvalid = new(
        "Billing.PaymentWebhookInvalid",
        "The payment notification could not be verified.",
        ErrorType.Forbidden);

    public static readonly Error PremiumAiSubscriptionRequired = new(
        "Billing.PremiumAiSubscriptionRequired",
        "Premium AI planning requires an active Traveler Premium subscription.",
        ErrorType.Forbidden);

    public static readonly Error AgencyMarketplaceSubscriptionRequired = new(
        "Billing.AgencyMarketplaceSubscriptionRequired",
        "An active Agency marketplace subscription is required for this operation.",
        ErrorType.Forbidden);

    public static readonly Error TravelerAiLimitReached = new(
        "Billing.TravelerAiLimitReached",
        "Your plan's AI itinerary limit has been reached for this billing period.",
        ErrorType.Forbidden);

    public static readonly Error AgencyPackageLimitReached = new(
        "Billing.AgencyPackageLimitReached",
        "Your plan's published package limit has been reached.",
        ErrorType.Forbidden);

    public static readonly Error InvalidPlanPrice = new(
        "Billing.InvalidPlanPrice",
        "The plan price must be zero or greater.",
        ErrorType.Validation);

    public static readonly Error InvalidCurrencyCode = new(
        "Billing.InvalidCurrencyCode",
        "Currency codes must use exactly three letters.",
        ErrorType.Validation);

    public static readonly Error InvalidPlanLimits = new(
        "Billing.InvalidPlanLimits",
        "Plan limits must be zero or greater.",
        ErrorType.Validation);

    public static readonly Error PaymentProviderNotConfigured = new(
        "Billing.PaymentProviderNotConfigured",
        "Stripe payment keys are not configured. Add Payments__StripeSecretKey and try again.",
        ErrorType.Validation);
}
