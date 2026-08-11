using FluentAssertions;
using Moq;
using Safarni.Application.Abstractions.Notifications;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Abstractions.Storage;
using Safarni.Application.Features.Packages;
using Safarni.Domain.Entities;
using Safarni.Domain.Enums;

namespace Safarni.Application.UnitTests.Marketplace;

public sealed class MarketplaceWorkflowTests
{
    [Fact]
    public async Task AddingDay_ShouldRejectDuplicateDayNumberWithinOwnedPackage()
    {
        var userId = Guid.NewGuid();
        var agency = new TravelAgency { Id = Guid.NewGuid(), UserId = userId };
        var package = new TravelPackage
        {
            Id = Guid.NewGuid(),
            TravelAgencyId = agency.Id,
            DurationDays = 3,
            TravelAgency = agency,
            Days = [new TravelPackageDay { Id = Guid.NewGuid(), DayNumber = 1, Title = "Arrival" }]
        };
        var agencies = new Mock<ITravelAgencyRepository>();
        agencies.Setup(value => value.GetByUserIdAsync(userId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(agency);
        var packages = new Mock<ITravelPackageRepository>();
        packages.Setup(value => value.GetByIdForAgencyAsync(package.Id, agency.Id, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(package);

        var handler = new AddTravelPackageDayCommandHandler(agencies.Object, packages.Object);
        var result = await handler.Handle(
            new AddTravelPackageDayCommand(userId, package.Id, 1, "Duplicate", "Details", null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("TravelPackage.DuplicateDayNumber");
        packages.Verify(value => value.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UploadingImage_ShouldRejectUnsupportedContentTypeBeforeStorage()
    {
        var storage = new Mock<ITravelPackageImageStorage>();
        var handler = new UploadTravelPackageImageCommandHandler(
            Mock.Of<ITravelAgencyRepository>(),
            Mock.Of<ITravelPackageRepository>(),
            storage.Object);

        var result = await handler.Handle(
            new UploadTravelPackageImageCommand(
                Guid.NewGuid(), Guid.NewGuid(), "cover.svg", "image/svg+xml", 10, [1, 2, 3]),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("TravelPackage.InvalidImage");
        storage.Verify(value => value.SaveAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AgencyRequestLookup_ShouldNotExposeRequestFromAnotherAgency()
    {
        var userId = Guid.NewGuid();
        var agency = new TravelAgency { Id = Guid.NewGuid(), UserId = userId };
        var agencies = new Mock<ITravelAgencyRepository>();
        agencies.Setup(value => value.GetByUserIdAsync(userId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(agency);
        var requests = new Mock<IPackageBookingRequestRepository>();
        requests.Setup(value => value.GetByIdForAgencyAsync(It.IsAny<Guid>(), agency.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PackageBookingRequest?)null);

        var handler = new GetAgencyBookingRequestQueryHandler(agencies.Object, requests.Object);
        var result = await handler.Handle(
            new GetAgencyBookingRequestQuery(userId, Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("PackageBooking.NotFound");
        requests.Verify(value => value.GetByIdForAgencyAsync(It.IsAny<Guid>(), agency.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BookingRequest_ShouldRejectUnpublishedPackage()
    {
        var package = new TravelPackage
        {
            Id = Guid.NewGuid(),
            IsPublished = false,
            IsActive = true,
            TravelAgency = new TravelAgency { VerificationStatus = AgencyVerificationStatus.Verified }
        };
        var packages = new Mock<ITravelPackageRepository>();
        packages.Setup(value => value.GetByIdAsync(package.Id, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(package);

        var handler = new CreatePackageBookingRequestCommandHandler(
            packages.Object,
            Mock.Of<IPackageBookingRequestRepository>(),
            Mock.Of<IMarketplaceNotificationService>());
        var result = await handler.Handle(
            new CreatePackageBookingRequestCommand(Guid.NewGuid(), package.Id, 2, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("PackageBooking.PackageUnavailable");
    }

    [Fact]
    public async Task PublishedPackageDetails_ShouldAllowVerifiedFreeAgencyWithoutSubscription()
    {
        var package = new TravelPackage
        {
            Id = Guid.NewGuid(),
            Title = "Cairo Weekend",
            Description = "A verified free-agency package",
            Destination = "Cairo",
            IsPublished = true,
            IsActive = true,
            TravelAgency = new TravelAgency
            {
                Id = Guid.NewGuid(),
                CompanyName = "Safarni Test Agency",
                VerificationStatus = AgencyVerificationStatus.Verified,
                Subscriptions = []
            },
            Days = []
        };
        package.TravelAgencyId = package.TravelAgency.Id;

        var packages = new Mock<ITravelPackageRepository>();
        packages.Setup(value => value.GetByIdAsync(package.Id, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(package);

        var handler = new GetPublishedPackageQueryHandler(packages.Object);
        var result = await handler.Handle(new GetPublishedPackageQuery(package.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(package.Id);
        result.Value.Agency.IsVerified.Should().BeTrue();
    }
}
