using FluentAssertions;
using Moq;
using Safarni.Application.Abstractions.Payments;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Features.Billing;
using Safarni.Application.Features.Packages;
using Safarni.Application.Abstractions.Security;
using Safarni.Domain.Entities;
using Safarni.Domain.Enums;

namespace Safarni.Application.UnitTests.Billing;

public sealed class BillingWorkflowTests
{
    [Fact]
    public async Task SuccessfulWebhook_ShouldActivateSubscriptionAndSetPeriod()
    {
        var plan = Plan(BillingPeriod.Monthly);
        var subscription = new Subscription { Id = Guid.NewGuid(), Plan = plan, PlanId = plan.Id, Status = SubscriptionStatus.Pending };
        var payment = new PaymentTransaction
        {
            Id = Guid.NewGuid(), Subscription = subscription, SubscriptionId = subscription.Id,
            Provider = "Test", PaymentSessionId = "session_1", Amount = 5, CurrencyCode = "USD"
        };
        var billing = new Mock<IBillingRepository>();
        billing.Setup(value => value.GetPaymentBySessionAsync("Test", "session_1", It.IsAny<CancellationToken>())).ReturnsAsync(payment);
        var handler = new ProcessPaymentWebhookCommandHandler(billing.Object);

        var result = await handler.Handle(new ProcessPaymentWebhookCommand(new PaymentWebhookEvent(
            "Test", "event_1", "session_1", PaymentStatus.Succeeded, null, DateTime.UtcNow)), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        subscription.Status.Should().Be(SubscriptionStatus.Active);
        subscription.StartAt.Should().NotBeNull();
        subscription.CurrentPeriodEnd.Should().BeCloseTo(DateTime.UtcNow.AddMonths(1), TimeSpan.FromSeconds(5));
        payment.Status.Should().Be(PaymentStatus.Succeeded);
        billing.Verify(value => value.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FailedWebhook_ShouldNeverGrantEntitlementState()
    {
        var plan = Plan(BillingPeriod.Yearly);
        var subscription = new Subscription { Id = Guid.NewGuid(), Plan = plan, PlanId = plan.Id, Status = SubscriptionStatus.Pending };
        var payment = new PaymentTransaction
        {
            Id = Guid.NewGuid(), Subscription = subscription, SubscriptionId = subscription.Id,
            Provider = "Test", PaymentSessionId = "session_2", Amount = 50, CurrencyCode = "USD"
        };
        var billing = new Mock<IBillingRepository>();
        billing.Setup(value => value.GetPaymentBySessionAsync("Test", "session_2", It.IsAny<CancellationToken>())).ReturnsAsync(payment);

        var result = await new ProcessPaymentWebhookCommandHandler(billing.Object).Handle(
            new ProcessPaymentWebhookCommand(new PaymentWebhookEvent(
                "Test", "event_2", "session_2", PaymentStatus.Failed, "declined", DateTime.UtcNow)), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        subscription.Status.Should().Be(SubscriptionStatus.PaymentFailed);
        subscription.CurrentPeriodEnd.Should().BeNull();
        payment.FailureReason.Should().Be("declined");
    }

    [Fact]
    public async Task DuplicateWebhook_ShouldBeIdempotent()
    {
        var plan = Plan(BillingPeriod.Monthly);
        var subscription = new Subscription { Id = Guid.NewGuid(), Plan = plan, PlanId = plan.Id, Status = SubscriptionStatus.Active };
        var payment = new PaymentTransaction
        {
            Id = Guid.NewGuid(), Subscription = subscription, SubscriptionId = subscription.Id,
            Provider = "Test", PaymentSessionId = "session_3", ProviderEventId = "event_3",
            Status = PaymentStatus.Succeeded
        };
        var billing = new Mock<IBillingRepository>();
        billing.Setup(value => value.GetPaymentBySessionAsync("Test", "session_3", It.IsAny<CancellationToken>())).ReturnsAsync(payment);

        var result = await new ProcessPaymentWebhookCommandHandler(billing.Object).Handle(
            new ProcessPaymentWebhookCommand(new PaymentWebhookEvent(
                "Test", "event_3", "session_3", PaymentStatus.Succeeded, null, DateTime.UtcNow)), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        billing.Verify(value => value.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task FreeAgencyPublish_BelowDailyLimit_ShouldSucceed()
    {
        var userId = Guid.NewGuid();
        var agency = new TravelAgency { Id = Guid.NewGuid(), UserId = userId, VerificationStatus = AgencyVerificationStatus.Verified };
        var package = new TravelPackage
        {
            Id = Guid.NewGuid(), TravelAgencyId = agency.Id, TravelAgency = agency, IsActive = true, DurationDays = 1
        };
        var city = new TravelPackageCity { Id = Guid.NewGuid(), TravelPackageId = package.Id, TravelPackage = package, OrderIndex = 0, Nights = 1, Name = "Cairo" };
        package.Cities.Add(city);
        package.Days.Add(new TravelPackageDay
        {
            Id = Guid.NewGuid(), TravelPackageId = package.Id, TravelPackage = package, DayNumber = 1,
            TravelPackageCityId = city.Id, TravelPackageCity = city, Title = "Cairo highlights", Description = "A complete first day."
        });
        var agencies = new Mock<ITravelAgencyRepository>();
        agencies.Setup(value => value.GetByUserIdAsync(userId, false, It.IsAny<CancellationToken>())).ReturnsAsync(agency);
        var packages = new Mock<ITravelPackageRepository>();
        packages.Setup(value => value.GetByIdForAgencyAsync(package.Id, agency.Id, true, It.IsAny<CancellationToken>())).ReturnsAsync(package);
        var verification = new Mock<ITravelAgencyAuthorizationService>();
        verification.Setup(value => value.IsVerifiedAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var entitlement = new Mock<IEntitlementService>();
        entitlement.Setup(value => value.GetAgencyPackageUsageAsync(agency.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsageEntitlement(true, 2, 3, DateTime.UtcNow.Date.AddDays(1), "agency-basic"));

        var result = await new PublishTravelPackageCommandHandler(agencies.Object, packages.Object, verification.Object, entitlement.Object)
            .Handle(new PublishTravelPackageCommand(userId, package.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        package.IsPublished.Should().BeTrue();
        package.PublishedAt.Should().NotBeNull();
        packages.Verify(value => value.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        entitlement.Verify(value => value.HasAgencyMarketplacePaidAsync(
            It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static PricingPlan Plan(BillingPeriod period) => new()
    {
        Id = Guid.NewGuid(), Code = "test", DisplayName = "Test", Description = "Test",
        CustomerType = PricingCustomerType.Traveler, BillingPeriod = period,
        Price = 5, CurrencyCode = "USD", BenefitsText = "Test", IsActive = true
    };
}
