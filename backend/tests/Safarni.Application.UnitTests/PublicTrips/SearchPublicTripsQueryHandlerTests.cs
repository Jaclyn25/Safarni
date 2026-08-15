using FluentAssertions;
using Moq;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Features.PublicTrips.Queries;
using Safarni.Domain.Entities;
using Safarni.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Safarni.Application.UnitTests.PublicTrips;

public class SearchPublicTripsQueryHandlerTests
{
    private readonly Mock<IPublicTripRepository> _repositoryMock;
    private readonly SearchPublicTripsQueryHandler _handler;

    public SearchPublicTripsQueryHandlerTests()
    {
        _repositoryMock = new Mock<IPublicTripRepository>();
        _handler = new SearchPublicTripsQueryHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_Should_ReturnMappedResponses()
    {
        // Arrange
        var query = new SearchPublicTripsQuery("Paris", 3, 7, 1000m, "newest");
        var snapshotId = Guid.NewGuid();
        var tripId = Guid.NewGuid();

        var trip = new Trip
        {
            Id = tripId,
            Name = "Paris Trip",
            Destination = "Paris",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(5),
            Visibility = TripVisibility.Public,
            Status = TripStatus.Completed,
            User = new User { Id = Guid.NewGuid(), FullName = "John Doe", IsActive = true }
        };

        var snapshot = new PublicTripSnapshot
        {
            Id = snapshotId,
            TripId = tripId,
            Trip = trip
        };

        var version = new SnapshotVersion
        {
            Id = Guid.NewGuid(),
            SnapshotId = snapshotId,
            IsActive = true,
            CostAmount = 500m,
            ViewsCount = 10,
            Snapshot = snapshot
        };

        _repositoryMock.Setup(repo => repo.SearchPublicTripsAsync(
            "Paris", 3, 7, 1000m, "newest", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SnapshotVersion> { version });

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        var response = result.Value!.First();
        response.TripName.Should().Be("Paris Trip");
        response.Destination.Should().Be("Paris");
        response.DurationDays.Should().Be(5);
        response.CostAmount.Should().Be(500m);
        response.AuthorName.Should().Be("John Doe");
        response.ViewsCount.Should().Be(10);
    }
}
