using FluentAssertions;
using Moq;
using Safarni.Application.Abstractions.Authentication;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Features.Trips;
using Safarni.Application.Features.Trips.UpdateTrip;
using Safarni.Domain.Entities;
using Xunit;

namespace Safarni.Application.UnitTests.Trips;

public class UpdateTripCommandHandlerTests
{
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<ITripRepository> _tripRepositoryMock;
    private readonly UpdateTripCommandHandler _handler;

    public UpdateTripCommandHandlerTests()
    {
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _tripRepositoryMock = new Mock<ITripRepository>();
        _handler = new UpdateTripCommandHandler(_currentUserServiceMock.Object, _tripRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_Should_ReturnSuccessResult_WhenCommandIsValid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tripId = Guid.NewGuid();
        var command = new UpdateTripCommand(
            tripId,
            "Updated Trip",
            "United Kingdom",
            DateTime.UtcNow.AddDays(2),
            DateTime.UtcNow.AddDays(8),
            "Egypt",
            [new TripCitySelection("London", 7)]);
        
        var trip = new Trip { Id = tripId, UserId = userId, Name = "Old", Destination = "Old" };

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        _tripRepositoryMock.Setup(x => x.GetByIdAsync(tripId, It.IsAny<CancellationToken>())).ReturnsAsync(trip);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Updated Trip");
        
        _tripRepositoryMock.Verify(
            x => x.SaveAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenTripDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tripId = Guid.NewGuid();
        var command = new UpdateTripCommand(
            tripId,
            "Updated Trip",
            "United Kingdom",
            DateTime.UtcNow.AddDays(2),
            DateTime.UtcNow.AddDays(8),
            "Egypt",
            [new TripCitySelection("London", 7)]);
        
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        _tripRepositoryMock.Setup(x => x.GetByIdAsync(tripId, It.IsAny<CancellationToken>())).ReturnsAsync((Trip?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Trip.NotFound");
    }

    [Fact]
    public async Task Handle_Should_ReturnForbidden_WhenUserIsNotOwner()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var tripId = Guid.NewGuid();
        var command = new UpdateTripCommand(
            tripId,
            "Updated Trip",
            "United Kingdom",
            DateTime.UtcNow.AddDays(2),
            DateTime.UtcNow.AddDays(8),
            "Egypt",
            [new TripCitySelection("London", 7)]);
        
        var trip = new Trip { Id = tripId, UserId = ownerId, Name = "Old", Destination = "Old" };

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        _tripRepositoryMock.Setup(x => x.GetByIdAsync(tripId, It.IsAny<CancellationToken>())).ReturnsAsync(trip);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Trip.Forbidden");
    }
}
