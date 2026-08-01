using FluentAssertions;
using Moq;
using Safarni.Application.Abstractions.Authentication;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Features.Trips;
using Safarni.Application.Features.Trips.ListTrips;
using Safarni.Domain.Entities;
using Xunit;

namespace Safarni.Application.UnitTests.Trips;

public class ListTripsQueryHandlerTests
{
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<ITripRepository> _tripRepositoryMock;
    private readonly ListTripsQueryHandler _handler;

    public ListTripsQueryHandlerTests()
    {
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _tripRepositoryMock = new Mock<ITripRepository>();
        _handler = new ListTripsQueryHandler(
            _currentUserServiceMock.Object,
            _tripRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_Should_ReturnTripsWithActiveItinerarySummary()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tripId = Guid.NewGuid();

        var trip = new Trip
        {
            Id = tripId,
            UserId = userId,
            Name = "Paris Trip",
            Destination = "France",
            OriginCountry = "Egypt",
            SelectedCitiesJson = """[{"name":"Paris","days":3}]""",
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(3),
            CreatedAt = DateTime.UtcNow,
            Cities =
            [
                new TripCity
                {
                    Id = Guid.NewGuid(),
                    TripId = tripId,
                    Name = "Paris",
                    Days = 3,
                    OrderIndex = 0
                }
            ],
            Itineraries =
            [
                new Itinerary
                {
                    Id = Guid.NewGuid(),
                    TripId = tripId,
                    CurrencyCode = "EUR",
                    Status = ItineraryStatus.Completed,
                    IsActive = true,
                    UpdatedAt = DateTime.UtcNow,
                    Days =
                    [
                        new ItineraryDay
                        {
                            DayNumber = 1,
                            Items =
                            [
                                new ItineraryItem
                                {
                                    Title = "Louvre",
                                    EstimatedCost = 120m
                                }
                            ]
                        }
                    ]
                },
                new Itinerary
                {
                    Id = Guid.NewGuid(),
                    TripId = tripId,
                    CurrencyCode = "EUR",
                    Status = ItineraryStatus.Failed,
                    IsActive = false,
                    UpdatedAt = DateTime.UtcNow
                }
            ]
        };

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        _tripRepositoryMock
            .Setup(x => x.ListByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([trip]);

        // Act
        var result = await _handler.Handle(
            new ListTripsQuery(),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var response = result.Value!.Single();
        response.Id.Should().Be(tripId);
        response.Name.Should().Be("Paris Trip");
        response.Cities.Single().Name.Should().Be("Paris");
        response.ActiveItineraryId.Should().Be(trip.Itineraries.First().Id);
        response.ItineraryStatus.Should().Be("completed");
        response.EstimatedTotalCost.Should().Be(120m);
    }

    [Fact]
    public async Task Handle_Should_ReturnNullItinerarySummary_WhenTripHasNoItinerary()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tripId = Guid.NewGuid();

        var trip = new Trip
        {
            Id = tripId,
            UserId = userId,
            Name = "Draft Trip",
            Destination = "Italy",
            OriginCountry = "Egypt",
            SelectedCitiesJson = "[]",
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(5),
            CreatedAt = DateTime.UtcNow,
            Cities =
            [
                new TripCity
                {
                    Id = Guid.NewGuid(),
                    TripId = tripId,
                    Name = "Rome",
                    Days = 5,
                    OrderIndex = 0
                }
            ]
        };

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        _tripRepositoryMock
            .Setup(x => x.ListByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([trip]);

        // Act
        var result = await _handler.Handle(
            new ListTripsQuery(),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var response = result.Value!.Single();
        response.ActiveItineraryId.Should().BeNull();
        response.ItineraryStatus.Should().BeNull();
        response.EstimatedTotalCost.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenUserIsNotAuthenticated()
    {
        // Arrange
        _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);

        // Act
        var result = await _handler.Handle(
            new ListTripsQuery(),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.AuthenticationRequired");
        _tripRepositoryMock.Verify(
            x => x.ListByUserIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
