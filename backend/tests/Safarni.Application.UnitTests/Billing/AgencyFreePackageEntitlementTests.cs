using FluentAssertions;
using Moq;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Abstractions.Security;
using Safarni.Application.Features.Packages;
using Safarni.Domain.Entities;

namespace Safarni.Application.UnitTests.Billing;

public sealed class AgencyFreePackageEntitlementTests
{
    [Fact]
    public async Task FreeAgencyAtDailyCreationLimit_ShouldRejectTwentyFirstPackage()
    {
        var userId = Guid.NewGuid();
        var agency = new TravelAgency { Id = Guid.NewGuid(), UserId = userId };
        var agencies = new Mock<ITravelAgencyRepository>();
        agencies.Setup(repository => repository.GetByUserIdAsync(
            userId, false, It.IsAny<CancellationToken>())).ReturnsAsync(agency);
        var packages = new Mock<ITravelPackageRepository>();
        var entitlements = new Mock<IEntitlementService>();
        entitlements.Setup(service => service.GetAgencyPackageCreationUsageAsync(
            agency.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsageEntitlement(false, 20, 20, DateTime.UtcNow.Date.AddDays(1), "agency-basic"));
        var handler = new CreateTravelPackageCommandHandler(
            agencies.Object, packages.Object, entitlements.Object);

        var result = await handler.Handle(new CreateTravelPackageCommand(
            userId,
            "Cairo weekend",
            "A complete Cairo weekend package.",
            "Cairo",
            DateTime.UtcNow.Date.AddDays(10),
            DateTime.UtcNow.Date.AddDays(12),
            3,
            500,
            "USD",
            10,
            null,
            null,
            null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Billing.AgencyPackageCreationLimitReached");
        packages.Verify(repository => repository.AddAsync(
            It.IsAny<TravelPackage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task FreeAgencyAtDailyPublicationLimit_ShouldRejectTwentyFirstPublication()
    {
        var userId = Guid.NewGuid();
        var agency = new TravelAgency { Id = Guid.NewGuid(), UserId = userId };
        var package = new TravelPackage
        {
            Id = Guid.NewGuid(),
            TravelAgencyId = agency.Id,
            TravelAgency = agency,
            IsActive = true
        };
        var agencies = new Mock<ITravelAgencyRepository>();
        agencies.Setup(repository => repository.GetByUserIdAsync(
            userId, false, It.IsAny<CancellationToken>())).ReturnsAsync(agency);
        var packages = new Mock<ITravelPackageRepository>();
        var authorization = new Mock<ITravelAgencyAuthorizationService>();
        authorization.Setup(service => service.IsVerifiedAsync(
            userId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var entitlements = new Mock<IEntitlementService>();
        entitlements.Setup(service => service.GetAgencyPackageUsageAsync(
            agency.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsageEntitlement(false, 20, 20, DateTime.UtcNow.Date.AddDays(1), "agency-basic"));
        var handler = new PublishTravelPackageCommandHandler(
            agencies.Object, packages.Object, authorization.Object, entitlements.Object);

        var result = await handler.Handle(
            new PublishTravelPackageCommand(userId, package.Id),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Billing.AgencyPackageLimitReached");
        packages.Verify(repository => repository.SaveChangesAsync(
            It.IsAny<CancellationToken>()), Times.Never);
    }
}
