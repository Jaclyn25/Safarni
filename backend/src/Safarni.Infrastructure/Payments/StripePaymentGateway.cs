using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Safarni.Application.Abstractions.Payments;
using Safarni.Domain.Enums;
using Stripe;
using Stripe.Checkout;

namespace Safarni.Infrastructure.Payments;

/// <summary>
/// Stripe Checkout integration for recurring Safarni subscriptions.
/// Secrets are intentionally read only from configuration/environment variables.
/// </summary>
public sealed class StripePaymentGateway : IPaymentGateway
{
    private readonly string _secretKey;
    private readonly string _webhookSecret;
    private readonly string _frontendBaseUrl;

    public StripePaymentGateway(IConfiguration configuration)
    {
        _secretKey = configuration["Payments:StripeSecretKey"] ?? string.Empty;
        _webhookSecret = configuration["Payments:StripeWebhookSecret"] ?? string.Empty;
        _frontendBaseUrl = configuration["Frontend:BaseUrl"] ?? "http://localhost:4200";
    }

    public async Task<PaymentCheckoutSession> CreateCheckoutSessionAsync(
        PaymentCheckoutRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured(_secretKey, "Payments:StripeSecretKey");

        StripeConfiguration.ApiKey = _secretKey;
        var metadata = request.Metadata.ToDictionary(item => item.Key, item => item.Value);
        var recurring = new SessionLineItemPriceDataRecurringOptions
        {
            Interval = request.BillingPeriod == BillingPeriod.Yearly ? "year" : "month"
        };

        var options = new SessionCreateOptions
        {
            Mode = "subscription",
            SuccessUrl = $"{_frontendBaseUrl.TrimEnd('/')}/pricing/checkout?status=success&session_id={{CHECKOUT_SESSION_ID}}",
            CancelUrl = $"{_frontendBaseUrl.TrimEnd('/')}/pricing/checkout?status=cancelled",
            ClientReferenceId = request.CustomerReference,
            CustomerEmail = request.CustomerEmail,
            Metadata = metadata,
            SubscriptionData = new SessionSubscriptionDataOptions { Metadata = metadata },
            LineItems =
            [
                new SessionLineItemOptions
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = request.CurrencyCode.ToLowerInvariant(),
                        UnitAmount = checked((long)decimal.Round(request.Amount * 100m, 0, MidpointRounding.AwayFromZero)),
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = request.Description
                        },
                        Recurring = recurring
                    }
                }
            ]
        };

        var session = await new SessionService().CreateAsync(options, cancellationToken: cancellationToken);
        return new PaymentCheckoutSession("Stripe", session.Id, session.Url);
    }

    public async Task<PaymentCheckoutSession> CreateOneTimeCheckoutSessionAsync(
        PaymentCheckoutRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured(_secretKey, "Payments:StripeSecretKey");

        StripeConfiguration.ApiKey = _secretKey;
        var metadata = request.Metadata.ToDictionary(item => item.Key, item => item.Value);
        var bookingReturnQuery = metadata.TryGetValue("bookingRequestId", out var bookingRequestId)
            ? $"&bookingRequestId={Uri.EscapeDataString(bookingRequestId)}"
            : string.Empty;
        var options = new SessionCreateOptions
        {
            Mode = "payment",
            SuccessUrl = $"{_frontendBaseUrl.TrimEnd('/')}/booking-payment?type=booking&status=success&session_id={{CHECKOUT_SESSION_ID}}{bookingReturnQuery}",
            CancelUrl = $"{_frontendBaseUrl.TrimEnd('/')}/booking-payment?type=booking&status=cancelled{bookingReturnQuery}",
            ClientReferenceId = request.CustomerReference,
            CustomerEmail = request.CustomerEmail,
            Metadata = metadata,
            LineItems =
            [
                new SessionLineItemOptions
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = request.CurrencyCode.ToLowerInvariant(),
                        UnitAmount = checked((long)decimal.Round(request.Amount * 100m, 0, MidpointRounding.AwayFromZero)),
                        ProductData = new SessionLineItemPriceDataProductDataOptions { Name = request.Description }
                    }
                }
            ]
        };

        var session = await new SessionService().CreateAsync(options, cancellationToken: cancellationToken);
        return new PaymentCheckoutSession("Stripe", session.Id, session.Url);
    }

    public async Task<string> GetOneTimeCheckoutUrlAsync(
        string provider,
        string sessionId,
        Guid bookingRequestId,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured(_secretKey, "Payments:StripeSecretKey");
        if (!string.Equals(provider, "Stripe", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The payment session belongs to another provider.");
        StripeConfiguration.ApiKey = _secretKey;
        var session = await new SessionService().GetAsync(sessionId, cancellationToken: cancellationToken);
        return session.Url;
    }

    public Task<PaymentWebhookEvent?> VerifyWebhookAsync(
        string payload,
        string signature,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured(_webhookSecret, "Payments:StripeWebhookSecret");
        if (string.IsNullOrWhiteSpace(signature))
        {
            throw new InvalidOperationException("Stripe webhook signature is required.");
        }

        try
        {
            // Stripe signs the exact raw request body. EventUtility also validates
            // the timestamp in Stripe-Signature to reduce replay attacks.
            EventUtility.ValidateSignature(payload, signature, _webhookSecret);
        }
        catch (StripeException exception)
        {
            throw new InvalidOperationException("Invalid Stripe webhook signature.", exception);
        }

        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;
        var eventType = GetString(root, "type");
        var eventId = GetString(root, "id");
        var dataObject = root.GetProperty("data").GetProperty("object");
        var occurredAt = FromUnixSeconds(GetLong(root, "created")) ?? DateTime.UtcNow;

        var webhook = eventType switch
        {
            "checkout.session.completed" => BuildCheckoutEvent(dataObject, eventId, occurredAt, PaymentStatus.Succeeded),
            "checkout.session.async_payment_failed" => BuildCheckoutEvent(dataObject, eventId, occurredAt, PaymentStatus.Failed),
            "checkout.session.expired" => BuildCheckoutEvent(dataObject, eventId, occurredAt, PaymentStatus.Failed),
            "invoice.paid" => BuildInvoiceEvent(dataObject, eventId, occurredAt, PaymentStatus.Succeeded),
            "invoice.payment_failed" => BuildInvoiceEvent(dataObject, eventId, occurredAt, PaymentStatus.Failed),
            _ => null
        };

        return Task.FromResult(webhook);
    }

    public async Task<PaymentRefundResult> RefundPaymentAsync(
        PaymentRefundRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured(_secretKey, "Payments:StripeSecretKey");
        StripeConfiguration.ApiKey = _secretKey;
        var session = await new SessionService().GetAsync(request.PaymentSessionId, cancellationToken: cancellationToken);
        if (string.IsNullOrWhiteSpace(session.PaymentIntentId))
            throw new InvalidOperationException("Stripe payment intent was not found for this checkout.");
        var refund = await new RefundService().CreateAsync(new RefundCreateOptions
        {
            PaymentIntent = session.PaymentIntentId,
            Amount = checked((long)decimal.Round(request.Amount * 100m, 0, MidpointRounding.AwayFromZero)),
            Reason = request.Reason.Length > 500 ? request.Reason[..500] : request.Reason
        }, cancellationToken: cancellationToken);
        return new PaymentRefundResult("Stripe", refund.Id);
    }

    public async Task ScheduleSubscriptionCancellationAsync(
        string providerSubscriptionId,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured(_secretKey, "Payments:StripeSecretKey");
        StripeConfiguration.ApiKey = _secretKey;
        await new SubscriptionService().UpdateAsync(
            providerSubscriptionId,
            new SubscriptionUpdateOptions { CancelAtPeriodEnd = true },
            cancellationToken: cancellationToken);
    }

    private static PaymentWebhookEvent BuildCheckoutEvent(
        JsonElement dataObject,
        string eventId,
        DateTime occurredAt,
        PaymentStatus status)
    {
        var sessionId = GetString(dataObject, "id");
        return new PaymentWebhookEvent(
            "Stripe",
            eventId,
            sessionId,
            status,
            status == PaymentStatus.Failed ? "Stripe Checkout payment was not completed." : null,
            occurredAt,
            GetString(dataObject, "subscription"),
            ToMajorAmount(GetLong(dataObject, "amount_total")),
            GetString(dataObject, "currency"));
    }

    private static PaymentWebhookEvent BuildInvoiceEvent(
        JsonElement dataObject,
        string eventId,
        DateTime occurredAt,
        PaymentStatus status)
    {
        var invoiceId = GetString(dataObject, "id");
        var amountInMinorUnits = GetLong(dataObject, "amount_paid") ?? GetLong(dataObject, "amount_due");
        var periodEnd = GetNestedLong(dataObject, "lines", "data", 0, "period", "end");
        return new PaymentWebhookEvent(
            "Stripe",
            eventId,
            invoiceId,
            status,
            status == PaymentStatus.Failed ? "Stripe recurring payment failed." : null,
            occurredAt,
            GetString(dataObject, "subscription"),
            ToMajorAmount(amountInMinorUnits),
            GetString(dataObject, "currency"),
            FromUnixSeconds(periodEnd));
    }

    private static string GetString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;

    private static long? GetLong(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var result)
            ? result
            : null;

    private static long? GetNestedLong(JsonElement element, string first, string second, int index, string fourth, string fifth)
    {
        if (!element.TryGetProperty(first, out var firstValue) ||
            !firstValue.TryGetProperty(second, out var secondValue) ||
            secondValue.ValueKind != JsonValueKind.Array ||
            secondValue.GetArrayLength() <= index)
        {
            return null;
        }

        var item = secondValue[index];
        return item.TryGetProperty(fourth, out var period) ? GetLong(period, fifth) : null;
    }

    private static decimal? ToMajorAmount(long? minorAmount) => minorAmount is null ? null : minorAmount.Value / 100m;

    private static DateTime? FromUnixSeconds(long? seconds) => seconds is null
        ? null
        : DateTimeOffset.FromUnixTimeSeconds(seconds.Value).UtcDateTime;

    private static void EnsureConfigured(string value, string settingName)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            value.Contains("your_", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("replace", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"{settingName} is not configured.");
        }
    }
}
