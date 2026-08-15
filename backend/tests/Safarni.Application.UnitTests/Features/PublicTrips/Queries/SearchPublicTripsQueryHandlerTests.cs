using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Configuration;
using Safarni.Application.Features.PublicTrips.Queries;
using Safarni.Domain.Entities;
using Safarni.Domain.Enums;
using Xunit;

namespace Safarni.Application.UnitTests.Features.PublicTrips.Queries;

public class SearchPublicTripsQueryHandlerTests
{
    private readonly Mock<IPublicTripRepository> _repositoryMock;
    private readonly SearchPublicTripsQueryHandler _handler;

    public SearchPublicTripsQueryHandlerTests()
    {
        _repositoryMock = new Mock<IPublicTripRepository>();
        var options = Options.Create(new PublicTripRankingOptions
        {
            RatingWeight = 0.25,
            VerificationWeight = 0.15,
            RecencyWeight = 0.15,
            EngagementWeight = 0.20,
            RelevanceWeight = 0.15,
            AuthorCredibilityWeight = 0.10,
            RecencyThresholdDays = 90,
            RecencyDecayRate = 0.01,
            DiversityScoreTolerance = 0.10
        });

        _handler = new SearchPublicTripsQueryHandler(_repositoryMock.Object, options);
    }

    [Fact]
    public async Task Handle_WithLessThen3Ratings_ShouldMarkAsNewTrip()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), IsActive = true };
        var trip = new Trip { Id = Guid.NewGuid(), User = user, PublishedAt = DateTime.UtcNow, StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(3), Destination = "Test" };
        var snapshot = new PublicTripSnapshot { Id = Guid.NewGuid(), Trip = trip };
        
        var versions = new List<SnapshotVersion>
        {
            new SnapshotVersion { Id = Guid.NewGuid(), Snapshot = snapshot, RatingsCount = 0, CreatedAt = DateTime.UtcNow },
            new SnapshotVersion { Id = Guid.NewGuid(), Snapshot = snapshot, RatingsCount = 2, CreatedAt = DateTime.UtcNow },
            new SnapshotVersion { Id = Guid.NewGuid(), Snapshot = snapshot, RatingsCount = 3, CreatedAt = DateTime.UtcNow }
        };

        _repositoryMock.Setup(r => r.SearchPublicTripsAsync(It.IsAny<string>(), null, null, null, null, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(versions);

        var query = new SearchPublicTripsQuery(null, null, null, null, null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var responses = result.Value.ToList();
        
        responses.Should().ContainSingle(r => r.VersionId == versions[0].Id && r.IsNewTrip == true);
        responses.Should().ContainSingle(r => r.VersionId == versions[1].Id && r.IsNewTrip == true);
        responses.Should().ContainSingle(r => r.VersionId == versions[2].Id && r.IsNewTrip == false);
    }

    [Fact]
    public async Task Handle_WithExplicitSortOrder_ShouldNotRank()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), IsActive = true };
        var trip = new Trip { Id = Guid.NewGuid(), User = user, PublishedAt = DateTime.UtcNow, StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(3), Destination = "Test" };
        var snapshot = new PublicTripSnapshot { Id = Guid.NewGuid(), Trip = trip };
        
        // Let's create two versions
        var version1 = new SnapshotVersion { Id = Guid.NewGuid(), Snapshot = snapshot, AverageRating = 1.0, RatingsCount = 5, CreatedAt = DateTime.UtcNow };
        var version2 = new SnapshotVersion { Id = Guid.NewGuid(), Snapshot = snapshot, AverageRating = 5.0, RatingsCount = 5, CreatedAt = DateTime.UtcNow };

        // Return them in a specific order mimicking "newest" or "popular" DB sort
        _repositoryMock.Setup(r => r.SearchPublicTripsAsync(It.IsAny<string>(), null, null, null, "newest", It.IsAny<CancellationToken>()))
                       .ReturnsAsync(new[] { version1, version2 });

        var query = new SearchPublicTripsQuery(null, null, null, null, "newest");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        var responses = result.Value.ToList();
        responses[0].VersionId.Should().Be(version1.Id); // Maintained original order
        responses[1].VersionId.Should().Be(version2.Id);
    }
}
