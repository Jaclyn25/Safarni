using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Safarni.Application.Abstractions.Payments;
using Safarni.Domain.Enums;

namespace Safarni.Infrastructure.Payments;

/// <summary>
/// Deterministic local payment provider. A real provider can implement the same
/// contract without changing pricing, subscription, or entitlement logic.
/// </summary>
public sealed class TestPaymentGateway : IPaymentGateway
{
    private readonly string _frontendBaseUrl;
    private readonly string _webhookSecret;

    public TestPaymentGateway(IConfiguration configuration)
    {
        _frontendBaseUrl = configuration["Frontend:BaseUrl"] ?? "http://localhost:4200";
        _webhookSecret = configuration["Payments:WebhookSecret"] ?? "safarni-test-webhook-secret";
    }

    public Task<PaymentCheckoutSession> CreateCheckoutSessionAsync(
        PaymentCheckoutRequest request,
        CancellationToken cancellationToken = default)
    {
        var sessionId = $"test_{Guid.NewGuid():N}";
        var checkoutUrl = $"{_frontendBaseUrl.TrimEnd('/')}/pricing/checkout?sessionId={sessionId}";
        return Task.FromResult(new PaymentCheckoutSession("Test", sessionId, checkoutUrl));
    }

    public Task<PaymentCheckoutSession> CreateOneTimeCheckoutSessionAsync(
        PaymentCheckoutRequest request,
        CancellationToken cancellationToken = default)
    {
        var sessionId = $"test_booking_{Guid.NewGuid():N}";
        var bookingRequestId = request.Metadata.TryGetValue("bookingRequestId", out var value)
            ? $"&bookingRequestId={Uri.EscapeDataString(value)}"
            : string.Empty;
        var checkoutUrl = $"{_frontendBaseUrl.TrimEnd('/')}/booking-payment?type=booking&sessionId={sessionId}{bookingRequestId}";
        return Task.FromResult(new PaymentCheckoutSession("Test", sessionId, checkoutUrl));
    }

    public Task<string> GetOneTimeCheckoutUrlAsync(
        string provider,
        string sessionId,
        Guid bookingRequestId,
        CancellationToken cancellationToken = default)
    {
        if (!string.Equals(provider, "Test", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The test payment session belongs to another provider.");
        return Task.FromResult($"{_frontendBaseUrl.TrimEnd('/')}/booking-payment?type=booking&sessionId={Uri.EscapeDataString(sessionId)}&bookingRequestId={bookingRequestId}");
    }

    public Task<PaymentWebhookEvent?> GetOneTimePaymentStatusAsync(
        string provider,
        string sessionId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<PaymentWebhookEvent?>(null);

    public Task<PaymentWebhookEvent?> VerifyWebhookAsync(
        string payload,
        string signature,
        CancellationToken cancellationToken = default)
    {
        var expected = Convert.ToHexString(HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(_webhookSecret), Encoding.UTF8.GetBytes(payload)));
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expected),
                Encoding.UTF8.GetBytes(signature.Trim())))
        {
            throw new InvalidOperationException("Invalid payment webhook signature.");
        }

        var webhook = JsonSerializer.Deserialize<TestWebhookPayload>(payload)
            ?? throw new InvalidOperationException("Invalid payment webhook payload.");
        if (string.IsNullOrWhiteSpace(webhook.EventId) || string.IsNullOrWhiteSpace(webhook.SessionId))
        {
            throw new InvalidOperationException("Payment webhook identifiers are required.");
        }

        return Task.FromResult<PaymentWebhookEvent?>(new PaymentWebhookEvent(
            "Test",
            webhook.EventId,
            webhook.SessionId,
            webhook.Success ? PaymentStatus.Succeeded : PaymentStatus.Failed,
            webhook.Success ? null : webhook.FailureReason ?? "Test payment failed.",
            webhook.OccurredAt ?? DateTime.UtcNow));
    }

    public Task<PaymentRefundResult> RefundPaymentAsync(
        PaymentRefundRequest request,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new PaymentRefundResult("Test", $"test_refund_{Guid.NewGuid():N}"));

    public Task ScheduleSubscriptionCancellationAsync(
        string providerSubscriptionId,
        CancellationToken cancellationToken = default) => Task.CompletedTask;

    public string CreateSignature(string payload) => Convert.ToHexString(HMACSHA256.HashData(
        Encoding.UTF8.GetBytes(_webhookSecret), Encoding.UTF8.GetBytes(payload)));

    private sealed record TestWebhookPayload(
        string EventId,
        string SessionId,
        bool Success,
        string? FailureReason,
        DateTime? OccurredAt);
}
