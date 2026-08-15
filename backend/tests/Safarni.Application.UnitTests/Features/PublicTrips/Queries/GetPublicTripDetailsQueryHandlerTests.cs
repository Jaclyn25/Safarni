using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Safarni.Application.Abstractions.Authentication;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Configuration;
using Safarni.Application.Common.Errors;
using Safarni.Application.Features.PublicTrips.Queries;
using Safarni.Domain.Entities;
using Safarni.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Safarni.Application.UnitTests.Features.PublicTrips.Queries;

public class GetPublicTripDetailsQueryHandlerTests
{
    private readonly Mock<IPublicTripRepository> _repositoryMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly IOptions<PublicTripViewsOptions> _options;
    private readonly GetPublicTripDetailsQueryHandler _handler;

    public GetPublicTripDetailsQueryHandlerTests()
    {
        _repositoryMock = new Mock<IPublicTripRepository>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();

        var opts = new PublicTripViewsOptions { DeduplicationWindowMinutes = 30 };
        _options = Options.Create(opts);

        _handler = new GetPublicTripDetailsQueryHandler(
            _repositoryMock.Object,
            _currentUserServiceMock.Object,
            _options);
    }

    [Fact]
    public async Task Handle_FirstAuthenticatedView_IncrementsViewCount()
    {
        // Arrange
        var versionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        _currentUserServiceMock.Setup(x => x.SessionId).Returns("session-id");

        var snapshot = new SnapshotVersion
        {
            Id = versionId,
            IsActive = true,
            ViewsCount = 5,
            Snapshot = new PublicTripSnapshot { Trip = new Trip { User = new User() } }
        };

        _repositoryMock.Setup(x => x.GetSnapshotVersionWithTripAndUserAsync(versionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(snapshot);

        // Simulate returning true indicating this is a new view
        _repositoryMock.Setup(x => x.TryRecordViewIfNotExistsAsync(versionId, userId, null, TimeSpan.FromMinutes(30), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(new GetPublicTripDetailsQuery(versionId), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ViewsCount.Should().Be(6); // Was 5, incremented to 6
        _repositoryMock.Verify(x => x.IncrementViewsCountAsync(versionId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_RefreshWithinWindow_DoesNotIncrement()
    {
        // Arrange
        var versionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        var snapshot = new SnapshotVersion
        {
            Id = versionId,
            IsActive = true,
            ViewsCount = 5,
            Snapshot = new PublicTripSnapshot { Trip = new Trip { User = new User() } }
        };

        _repositoryMock.Setup(x => x.GetSnapshotVersionWithTripAndUserAsync(versionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(snapshot);

        // Simulate returning false indicating this view already exists recently
        _repositoryMock.Setup(x => x.TryRecordViewIfNotExistsAsync(versionId, userId, null, TimeSpan.FromMinutes(30), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(new GetPublicTripDetailsQuery(versionId), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ViewsCount.Should().Be(5); // Remained 5
        _repositoryMock.Verify(x => x.IncrementViewsCountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_FirstAnonymousSessionView_IncrementsViewCount()
    {
        // Arrange
        var versionId = Guid.NewGuid();
        var sessionId = "anonymous-session-abc";
        
        _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);
        _currentUserServiceMock.Setup(x => x.SessionId).Returns(sessionId);

        var snapshot = new SnapshotVersion
        {
            Id = versionId,
            IsActive = true,
            ViewsCount = 10,
            Snapshot = new PublicTripSnapshot { Trip = new Trip { User = new User() } }
        };

        _repositoryMock.Setup(x => x.GetSnapshotVersionWithTripAndUserAsync(versionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(snapshot);

        _repositoryMock.Setup(x => x.TryRecordViewIfNotExistsAsync(versionId, null, sessionId, TimeSpan.FromMinutes(30), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(new GetPublicTripDetailsQuery(versionId), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ViewsCount.Should().Be(11);
        _repositoryMock.Verify(x => x.IncrementViewsCountAsync(versionId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
