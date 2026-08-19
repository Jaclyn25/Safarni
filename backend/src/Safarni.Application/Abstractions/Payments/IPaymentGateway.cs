using Safarni.Domain.Enums;

namespace Safarni.Application.Abstractions.Payments;

public sealed record PaymentCheckoutRequest(
    string CustomerReference,
    decimal Amount,
    string CurrencyCode,
    string Description,
    IReadOnlyDictionary<string, string> Metadata,
    BillingPeriod BillingPeriod = BillingPeriod.Monthly,
    string? CustomerEmail = null);

public sealed record PaymentCheckoutSession(
    string Provider,
    string SessionId,
    string CheckoutUrl);

public sealed record PaymentWebhookEvent(
    string Provider,
    string EventId,
    string SessionId,
    PaymentStatus Status,
    string? FailureReason,
    DateTime OccurredAt,
    string? ProviderSubscriptionId = null,
    decimal? Amount = null,
    string? CurrencyCode = null,
    DateTime? CurrentPeriodEnd = null);

public sealed record PaymentRefundRequest(
    string Provider,
    string PaymentSessionId,
    decimal Amount,
    string CurrencyCode,
    string Reason);

public sealed record PaymentRefundResult(string Provider, string RefundId);

public interface IPaymentGateway
{
    Task<PaymentCheckoutSession> CreateCheckoutSessionAsync(
        PaymentCheckoutRequest request,
        CancellationToken cancellationToken = default);

    Task<PaymentCheckoutSession> CreateOneTimeCheckoutSessionAsync(
        PaymentCheckoutRequest request,
        CancellationToken cancellationToken = default);

    Task<string> GetOneTimeCheckoutUrlAsync(
        string provider,
        string sessionId,
        Guid bookingRequestId,
        CancellationToken cancellationToken = default);

    Task<PaymentWebhookEvent?> GetOneTimePaymentStatusAsync(
        string provider,
        string sessionId,
        CancellationToken cancellationToken = default);

    Task<PaymentWebhookEvent?> VerifyWebhookAsync(
        string payload,
        string signature,
        CancellationToken cancellationToken = default);

    Task<PaymentRefundResult> RefundPaymentAsync(
        PaymentRefundRequest request,
        CancellationToken cancellationToken = default);

    Task ScheduleSubscriptionCancellationAsync(
        string providerSubscriptionId,
        CancellationToken cancellationToken = default);
}
