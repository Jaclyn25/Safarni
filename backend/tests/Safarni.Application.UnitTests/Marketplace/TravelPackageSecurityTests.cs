using FluentAssertions;
using Moq;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Abstractions.Security;
using Safarni.Application.Features.Packages;
using Safarni.Domain.Entities;

namespace Safarni.Application.UnitTests.Marketplace;

public sealed class TravelPackageSecurityTests
{
    [Fact]
    public async Task Publishing_ShouldBeBlocked_WhenAgencyIsNotVerified()
    {
        var userId = Guid.NewGuid();
        var agency = new TravelAgency { Id = Guid.NewGuid(), UserId = userId };
        var agencies = new Mock<ITravelAgencyRepository>();
        agencies.Setup(value => value.GetByUserIdAsync(userId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(agency);
        var authorization = new Mock<ITravelAgencyAuthorizationService>();
        authorization.Setup(value => value.IsVerifiedAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = new PublishTravelPackageCommandHandler(
            agencies.Object,
            Mock.Of<ITravelPackageRepository>(),
            authorization.Object);

        var result = await handler.Handle(
            new PublishTravelPackageCommand(userId, Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("TravelPackage.VerifiedAgencyRequired");
    }

    [Fact]
    public async Task Updating_ShouldRejectAPackageOwnedByAnotherAgency()
    {
        var userId = Guid.NewGuid();
        var packageId = Guid.NewGuid();
        var agency = new TravelAgency { Id = Guid.NewGuid(), UserId = userId };
        var otherPackage = new TravelPackage
        {
            Id = packageId,
            TravelAgencyId = Guid.NewGuid(),
            TravelAgency = new TravelAgency { Id = Guid.NewGuid() }
        };
        var agencies = new Mock<ITravelAgencyRepository>();
        agencies.Setup(value => value.GetByUserIdAsync(userId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(agency);
        var packages = new Mock<ITravelPackageRepository>();
        packages.Setup(value => value.GetByIdForAgencyAsync(packageId, agency.Id, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TravelPackage?)null);
        packages.Setup(value => value.GetByIdAsync(packageId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(otherPackage);

        var handler = new UpdateTravelPackageCommandHandler(agencies.Object, packages.Object);
        var result = await handler.Handle(
            new UpdateTravelPackageCommand(userId, packageId, "Title", "Description", "Egypt",
                new DateTime(2026, 8, 10), new DateTime(2026, 8, 12), 3, 100, "USD", 10, null, null, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("TravelPackage.OwnershipDenied");
    }
}
