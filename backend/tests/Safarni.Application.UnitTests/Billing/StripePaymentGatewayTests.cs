using System.Globalization;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Safarni.Domain.Enums;
using Safarni.Infrastructure.Payments;
using Stripe;

namespace Safarni.Application.UnitTests.Billing;

public sealed class StripePaymentGatewayTests
{
    [Fact]
    public async Task VerifyWebhookAsync_ShouldAcceptSignedEventFromNewerApiReleaseTrain()
    {
        const string webhookSecret = "whsec_test_secret";
        const string payload = """
            {
              "id": "evt_dahlia_checkout",
              "type": "checkout.session.completed",
              "api_version": "2026-07-29.dahlia",
              "created": 1786417200,
              "data": {
                "object": {
                  "id": "cs_test_safarni",
                  "subscription": "sub_test_safarni",
                  "amount_total": 500,
                  "currency": "usd"
                }
              }
            }
            """;

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Payments:StripeWebhookSecret"] = webhookSecret
            })
            .Build();
        var gateway = new StripePaymentGateway(configuration);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        var signature = EventUtility.ComputeSignature(webhookSecret, timestamp, payload);

        var result = await gateway.VerifyWebhookAsync(
            payload,
            $"t={timestamp},v1={signature}",
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.EventId.Should().Be("evt_dahlia_checkout");
        result.SessionId.Should().Be("cs_test_safarni");
        result.ProviderSubscriptionId.Should().Be("sub_test_safarni");
        result.Status.Should().Be(PaymentStatus.Succeeded);
        result.Amount.Should().Be(5m);
        result.CurrencyCode.Should().Be("usd");
    }
}
