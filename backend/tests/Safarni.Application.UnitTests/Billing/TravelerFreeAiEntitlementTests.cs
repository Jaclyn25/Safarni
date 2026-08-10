using FluentAssertions;
using Moq;
using Safarni.Application.Abstractions.Authentication;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Abstractions.Security;
using Safarni.Application.Features.Trips.GenerateItinerary;
using Safarni.Application.Features.Trips.GenerateItinerary.Jobs;
using Safarni.Domain.Entities;

namespace Safarni.Application.UnitTests.Billing;

public sealed class TravelerFreeAiEntitlementTests
{
    [Fact]
    public async Task FreeTravelerBelowDailyLimit_ShouldQueueGeneration()
    {
        var fixture = CreateFixture(new UsageEntitlement(
            true,
            2,
            3,
            StartOfNextDayUtc(),
            "traveler-basic"));

        var result = await fixture.Handler.Handle(
            CreateCommand(fixture.Trip.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        fixture.Entitlements.Verify(service => service.GetTravelerAiUsageAsync(
            fixture.Trip.UserId,
            It.IsAny<CancellationToken>()), Times.Once);
        fixture.Entitlements.Verify(service => service.HasTravelerPremiumAiAsync(
            It.IsAny<Guid>(),
            It.IsAny<CancellationToken>()), Times.Never);
        fixture.Jobs.Verify(repository => repository.TryAddAsync(
            It.Is<ItineraryGenerationJob>(job =>
                job.UserId == fixture.Trip.UserId &&
                job.TripId == fixture.Trip.Id),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FreeTravelerAtDailyLimit_ShouldRejectFourthGeneration()
    {
        var fixture = CreateFixture(new UsageEntitlement(
            false,
            3,
            3,
            StartOfNextDayUtc(),
            "traveler-basic"));

        var result = await fixture.Handler.Handle(
            CreateCommand(fixture.Trip.Id),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Billing.TravelerAiLimitReached");
        fixture.Jobs.Verify(repository => repository.TryAddAsync(
            It.IsAny<ItineraryGenerationJob>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Fixture CreateFixture(UsageEntitlement entitlement)
    {
        var userId = Guid.NewGuid();
        var trip = new Trip
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = "Free AI trip",
            Destination = "Cairo",
            OriginCountry = "Egypt",
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date
        };
        trip.Cities.Add(new TripCity
        {
            Id = Guid.NewGuid(),
            TripId = trip.Id,
            Trip = trip,
            Name = "Cairo",
            Days = 1,
            OrderIndex = 0
        });

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(service => service.UserId).Returns(userId);

        var trips = new Mock<ITripRepository>();
        trips.Setup(repository => repository.GetByIdAsync(
                trip.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(trip);

        var jobs = new Mock<IItineraryGenerationJobRepository>();
        jobs.Setup(repository => repository.HasActiveJobAsync(
                trip.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        jobs.Setup(repository => repository.TryAddAsync(
                It.IsAny<ItineraryGenerationJob>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var entitlements = new Mock<IEntitlementService>();
        entitlements.Setup(service => service.GetTravelerAiUsageAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(entitlement);

        var handler = new StartItineraryGenerationCommandHandler(
            currentUser.Object,
            trips.Object,
            jobs.Object,
            queueSignal: null,
            entitlements: entitlements.Object);

        return new Fixture(handler, trip, jobs, entitlements);
    }

    private static StartItineraryGenerationCommand CreateCommand(Guid tripId) =>
        new(
            tripId,
            new GenerateItineraryPreferences
            {
                Interests = ["culture"],
                Pace = "balanced",
                Transportation = ["walking"],
                AccommodationStyle = "mid_range"
            },
            BudgetAmount: null,
            CurrencyCode: "USD",
            TravelerCount: 1,
            IdempotencyKey: null);

    private static DateTime StartOfNextDayUtc()
        => DateTime.UtcNow.Date.AddDays(1);

    private sealed record Fixture(
        StartItineraryGenerationCommandHandler Handler,
        Trip Trip,
        Mock<IItineraryGenerationJobRepository> Jobs,
        Mock<IEntitlementService> Entitlements);
}
