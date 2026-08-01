using FluentAssertions;
using Moq;
using Safarni.Application.Abstractions.Authentication;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Errors;
using Safarni.Application.Features.Trips;
using Safarni.Application.Features.Trips.GetTrip;
using Safarni.Domain.Entities;
using Xunit;

namespace Safarni.Application.UnitTests.Trips;

public class GetTripQueryHandlerTests
{
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<ITripRepository> _tripRepositoryMock;
    private readonly GetTripQueryHandler _handler;

    public GetTripQueryHandlerTests()
    {
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _tripRepositoryMock = new Mock<ITripRepository>();
        _handler = new GetTripQueryHandler(
            _currentUserServiceMock.Object,
            _tripRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_Should_ReturnTrip_WhenUserOwnsIt()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tripId = Guid.NewGuid();
        var trip = new Trip
        {
            Id = tripId,
            UserId = userId,
            Name = "Rome Trip",
            Destination = "Italy",
            OriginCountry = "Egypt",
            SelectedCitiesJson = "[]",
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(4),
            CreatedAt = DateTime.UtcNow,
            Cities =
            [
                new TripCity
                {
                    Id = Guid.NewGuid(),
                    TripId = tripId,
                    Name = "Rome",
                    Days = 4,
                    OrderIndex = 0
                }
            ]
        };

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        _tripRepositoryMock
            .Setup(x => x.GetByIdWithItinerariesAsync(tripId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(trip);

        // Act
        var result = await _handler.Handle(
            new GetTripQuery(tripId),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(tripId);
        result.Value.Name.Should().Be("Rome Trip");
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenTripDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tripId = Guid.NewGuid();

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        _tripRepositoryMock
            .Setup(x => x.GetByIdWithItinerariesAsync(tripId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Trip?)null);

        // Act
        var result = await _handler.Handle(
            new GetTripQuery(tripId),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Trip.NotFound");
    }

    [Fact]
    public async Task Handle_Should_ReturnForbidden_WhenUserDoesNotOwnTrip()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tripId = Guid.NewGuid();
        var trip = new Trip
        {
            Id = tripId,
            UserId = Guid.NewGuid(),
            Name = "Someone Else's Trip",
            Destination = "Italy",
            OriginCountry = "Egypt",
            SelectedCitiesJson = "[]",
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(4),
            CreatedAt = DateTime.UtcNow
        };

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        _tripRepositoryMock
            .Setup(x => x.GetByIdWithItinerariesAsync(tripId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(trip);

        // Act
        var result = await _handler.Handle(
            new GetTripQuery(tripId),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var tripId = Guid.NewGuid();
        _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);

        // Act
        var result = await _handler.Handle(
            new GetTripQuery(tripId),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.AuthenticationRequired");
    }
}
