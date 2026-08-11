using FluentAssertions;
using Moq;
using Safarni.Application.Abstractions.Notifications;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Abstractions.Security;
using Safarni.Application.Abstractions.Storage;
using Safarni.Application.Abstractions.Travel;
using Safarni.Application.Features.Packages;
using Safarni.Domain.Entities;
using Safarni.Domain.Enums;
using ChatBotApi.Application.Common.Models.GooglePlaces;

namespace Safarni.Application.UnitTests.Marketplace;

public sealed class MarketplaceWorkflowTests
{
    [Fact]
    public void PackageResponse_ShouldUseGalleryCoverAsTheCanonicalImage()
    {
        var agency = new TravelAgency
        {
            Id = Guid.NewGuid(),
            CompanyName = "Safarni Test Agency"
        };
        var package = new TravelPackage
        {
            Id = Guid.NewGuid(),
            TravelAgencyId = agency.Id,
            TravelAgency = agency,
            Title = "Paris Highlights",
            Description = "A complete city package",
            Destination = "France",
            CoverImageUrl = "/uploads/travel-packages/old-ghost.jpg",
            Images =
            [
                new TravelPackageImage
                {
                    Id = Guid.NewGuid(),
                    Url = "/uploads/travel-packages/old-ghost.jpg",
                    SortOrder = 0,
                    IsCover = false
                },
                new TravelPackageImage
                {
                    Id = Guid.NewGuid(),
                    Url = "/uploads/travel-packages/new-suitcase.jpg",
                    SortOrder = 1,
                    IsCover = true
                }
            ]
        };

        var response = TravelPackageMapping.ToResponse(package);

        response.CoverImageUrl.Should().Be("/uploads/travel-packages/new-suitcase.jpg");
        response.Images.Should().HaveCount(2);
        response.Images[0].Url.Should().Be("/uploads/travel-packages/new-suitcase.jpg");
        response.Images[0].IsCover.Should().BeTrue();
    }

    [Fact]
    public async Task Publishing_ShouldExplainInvalidRouteNightCoverage()
    {
        var userId = Guid.NewGuid();
        var agency = new TravelAgency { Id = Guid.NewGuid(), UserId = userId };
        var city = new TravelPackageCity { Id = Guid.NewGuid(), Name = "Cairo", OrderIndex = 0, Nights = 1 };
        var package = new TravelPackage
        {
            Id = Guid.NewGuid(),
            TravelAgencyId = agency.Id,
            TravelAgency = agency,
            IsActive = true,
            DurationDays = 3,
            Cities = [city]
        };
        var agencies = new Mock<ITravelAgencyRepository>();
        agencies.Setup(value => value.GetByUserIdAsync(userId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(agency);
        var packages = new Mock<ITravelPackageRepository>();
        packages.Setup(value => value.GetByIdForAgencyAsync(package.Id, agency.Id, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(package);
        var authorization = new Mock<ITravelAgencyAuthorizationService>();
        authorization.Setup(value => value.IsVerifiedAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await new PublishTravelPackageCommandHandler(
                agencies.Object, packages.Object, authorization.Object)
            .Handle(new PublishTravelPackageCommand(userId, package.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("TravelPackage.InvalidNightAllocation");
        packages.Verify(value => value.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddingRouteStop_ShouldExplicitlyInsertNewCity()
    {
        var userId = Guid.NewGuid();
        var agency = new TravelAgency { Id = Guid.NewGuid(), UserId = userId, CompanyName = "Test Agency" };
        var package = new TravelPackage
        {
            Id = Guid.NewGuid(),
            TravelAgencyId = agency.Id,
            TravelAgency = agency,
            DurationDays = 3,
            Title = "Egypt highlights",
            Description = "A complete package",
            Destination = "Egypt"
        };
        var agencies = new Mock<ITravelAgencyRepository>();
        agencies.Setup(value => value.GetByUserIdAsync(userId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(agency);
        var packages = new Mock<ITravelPackageRepository>();
        packages.Setup(value => value.GetByIdForAgencyAsync(package.Id, agency.Id, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(package);
        packages.Setup(value => value.AddCityAsync(It.IsAny<TravelPackageCity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await new SaveTravelPackageCityCommandHandler(agencies.Object, packages.Object)
            .Handle(new SaveTravelPackageCityCommand(
                userId, package.Id, null, "Cairo", 1, "Historic Cairo"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        package.Cities.Should().ContainSingle(city => city.Name == "Cairo" && city.OrderIndex == 0);
        packages.Verify(value => value.AddCityAsync(
            It.Is<TravelPackageCity>(city => city.Name == "Cairo"), It.IsAny<CancellationToken>()), Times.Once);
        packages.Verify(value => value.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void RoutePlanner_ShouldDeriveEveryDayCityFromOrderedNights()
    {
        var cairo = new TravelPackageCity { Id = Guid.NewGuid(), Name = "Cairo", OrderIndex = 0, Nights = 2 };
        var hurghada = new TravelPackageCity { Id = Guid.NewGuid(), Name = "Hurghada", OrderIndex = 1, Nights = 2 };
        var package = new TravelPackage
        {
            DurationDays = 5,
            Cities = [cairo, hurghada],
            Days = Enumerable.Range(1, 5)
                .Select(number => new TravelPackageDay { Id = Guid.NewGuid(), DayNumber = number })
                .ToList()
        };

        TravelPackageRoutePlanner.AssignCitiesToDays(package);

        package.Days.OrderBy(day => day.DayNumber).Select(day => day.TravelPackageCity?.Name)
            .Should().Equal("Cairo", "Cairo", "Hurghada", "Hurghada", "Hurghada");
        TravelPackageRoutePlanner.HasValidNightAllocation(package).Should().BeTrue();
    }

    [Fact]
    public async Task PlaceSearch_ShouldUseRouteDerivedCityInsteadOfStaleDayRelation()
    {
        var userId = Guid.NewGuid();
        var agency = new TravelAgency { Id = Guid.NewGuid(), UserId = userId };
        var cairo = new TravelPackageCity { Id = Guid.NewGuid(), Name = "Cairo", OrderIndex = 0, Nights = 1 };
        var hurghada = new TravelPackageCity { Id = Guid.NewGuid(), Name = "Hurghada", OrderIndex = 1, Nights = 2 };
        var day = new TravelPackageDay
        {
            Id = Guid.NewGuid(),
            DayNumber = 2,
            TravelPackageCityId = cairo.Id,
            TravelPackageCity = cairo
        };
        var package = new TravelPackage
        {
            Id = Guid.NewGuid(),
            TravelAgencyId = agency.Id,
            TravelAgency = agency,
            DurationDays = 3,
            CurrencyCode = "USD",
            Cities = [cairo, hurghada],
            Days = [day]
        };
        var agencies = new Mock<ITravelAgencyRepository>();
        agencies.Setup(value => value.GetByUserIdAsync(userId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(agency);
        var packages = new Mock<ITravelPackageRepository>();
        packages.Setup(value => value.GetByIdForAgencyAsync(package.Id, agency.Id, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(package);
        var travelData = new Mock<IItineraryTravelDataService>();
        travelData.Setup(value => value.SearchLocalAsync(
                "popular restaurants in Hurghada", "Hurghada", "en", It.IsAny<CancellationToken>()))
            .ReturnsAsync([new PlaceDetails { PlaceId = "api-1", Name = "Marina Restaurant", Address = "Hurghada Marina" }]);

        var result = await new SearchPackagePlacesQueryHandler(agencies.Object, packages.Object, travelData.Object)
            .Handle(new SearchPackagePlacesQuery(
                userId, package.Id, day.Id, string.Empty, TravelPackageItemType.Restaurant), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(value => value.Name == "Marina Restaurant");
        travelData.VerifyAll();
    }

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
            new AddTravelPackageDayCommand(userId, package.Id, 1, "Duplicate", "Details"),
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
    public async Task UploadingGalleryImage_ShouldExplicitlyInsertNewImage()
    {
        var userId = Guid.NewGuid();
        var agency = new TravelAgency { Id = Guid.NewGuid(), UserId = userId, CompanyName = "Test Agency" };
        var package = new TravelPackage
        {
            Id = Guid.NewGuid(), TravelAgencyId = agency.Id, TravelAgency = agency,
            Title = "Cairo Weekend", Description = "Package details", Destination = "Cairo"
        };
        var agencies = new Mock<ITravelAgencyRepository>();
        agencies.Setup(value => value.GetByUserIdAsync(userId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(agency);
        var packages = new Mock<ITravelPackageRepository>();
        packages.Setup(value => value.GetByIdForAgencyAsync(package.Id, agency.Id, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(package);
        packages.Setup(value => value.AddImageAsync(It.IsAny<TravelPackageImage>(), It.IsAny<CancellationToken>()))
            .Callback<TravelPackageImage, CancellationToken>((image, _) => package.Images.Add(image))
            .Returns(Task.CompletedTask);
        var storage = new Mock<ITravelPackageImageStorage>();
        storage.Setup(value => value.SaveAsync(It.IsAny<Stream>(), ".jpg", It.IsAny<CancellationToken>()))
            .ReturnsAsync("/uploads/travel-packages/new-cover.jpg");

        var handler = new UploadTravelPackageGalleryImageCommandHandler(agencies.Object, packages.Object, storage.Object);
        var result = await handler.Handle(new UploadTravelPackageGalleryImageCommand(
            userId, package.Id, null, null, null, "Cairo package cover", true,
            "cover.jpg", "image/jpeg", 3, [1, 2, 3]), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        package.CoverImageUrl.Should().Be("/uploads/travel-packages/new-cover.jpg");
        package.Images.Should().ContainSingle(image => image.IsCover && image.TravelPackageId == package.Id);
        packages.Verify(value => value.AddImageAsync(It.IsAny<TravelPackageImage>(), It.IsAny<CancellationToken>()), Times.Once);
        packages.Verify(value => value.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
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
            EndDate = DateTime.UtcNow.Date.AddDays(2),
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
