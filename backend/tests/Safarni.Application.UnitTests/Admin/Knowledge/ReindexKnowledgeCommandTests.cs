using ChatBotApi.Application.Common.Interfaces;
using FluentAssertions;
using Moq;
using Safarni.Application.Abstractions.Auditing;
using Safarni.Application.Abstractions.Authentication;
using Safarni.Application.Features.Admin.Common;
using Safarni.Application.Features.Admin.Knowledge;
using Safarni.Domain.Enums;

namespace Safarni.Application.UnitTests.Admin.Knowledge;

public class ReindexKnowledgeCommandTests
{
    private static readonly Guid ActorId = Guid.NewGuid();

    private static Mock<ICurrentUserService> CreateCurrentUser()
    {
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(x => x.UserId).Returns(ActorId);
        currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);
        return currentUser;
    }

    private static Mock<IAuditLogWriter> CreateAuditWriter()
    {
        var writer = new Mock<IAuditLogWriter>();
        writer
            .Setup(x => x.WriteAsync(
                It.IsAny<Guid?>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<object?>(),
                It.IsAny<AuditSeverity>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return writer;
    }

    [Fact]
    public async Task Handle_WhenQdrantUnreachable_ShouldFailAndNotReindex()
    {
        var qdrantService = new Mock<IQdrantService>();
        qdrantService
            .Setup(x => x.IsHealthyAsync())
            .ReturnsAsync(false);

        var indexingService = new Mock<IIndexingService>();
        var auditWriter = CreateAuditWriter();

        var handler = new ReindexKnowledgeCommandHandler(
            qdrantService.Object,
            indexingService.Object,
            auditWriter.Object,
            CreateCurrentUser().Object);

        var result = await handler.Handle(
            new ReindexKnowledgeCommand(),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(AdminErrors.QdrantUnavailable.Code);
        indexingService.Verify(
            x => x.ReindexAllAsync(),
            Times.Never);
        auditWriter.Verify(
            x => x.WriteAsync(
                It.IsAny<Guid?>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<object?>(),
                It.IsAny<AuditSeverity>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenHealthCheckThrows_ShouldTreatAsUnavailable()
    {
        var qdrantService = new Mock<IQdrantService>();
        qdrantService
            .Setup(x => x.IsHealthyAsync())
            .ThrowsAsync(new InvalidOperationException("connection refused"));

        var handler = new ReindexKnowledgeCommandHandler(
            qdrantService.Object,
            new Mock<IIndexingService>().Object,
            CreateAuditWriter().Object,
            CreateCurrentUser().Object);

        var result = await handler.Handle(
            new ReindexKnowledgeCommand(),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(AdminErrors.QdrantUnavailable.Code);
    }

    [Fact]
    public async Task Handle_WhenSuccessful_ShouldReturnChunkCountAndAudit()
    {
        var qdrantService = new Mock<IQdrantService>();
        qdrantService
            .Setup(x => x.IsHealthyAsync())
            .ReturnsAsync(true);

        var indexingService = new Mock<IIndexingService>();
        indexingService
            .Setup(x => x.ReindexAllAsync())
            .ReturnsAsync(37);

        var auditWriter = CreateAuditWriter();

        var handler = new ReindexKnowledgeCommandHandler(
            qdrantService.Object,
            indexingService.Object,
            auditWriter.Object,
            CreateCurrentUser().Object);

        var result = await handler.Handle(
            new ReindexKnowledgeCommand(),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Success.Should().BeTrue();
        result.Value.ChunkCount.Should().Be(37);
        result.Value.QdrantHealthy.Should().BeTrue();

        auditWriter.Verify(
            x => x.WriteAsync(
                ActorId,
                AdminAuditActions.KnowledgeReindexed,
                "Knowledge",
                null,
                It.IsAny<object?>(),
                AuditSeverity.Info,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenReindexThrows_ShouldReturnFailureResponseAndAuditError()
    {
        var qdrantService = new Mock<IQdrantService>();
        qdrantService
            .Setup(x => x.IsHealthyAsync())
            .ReturnsAsync(true);

        var indexingService = new Mock<IIndexingService>();
        indexingService
            .Setup(x => x.ReindexAllAsync())
            .ThrowsAsync(new Exception("Qdrant write timeout"));

        var auditWriter = CreateAuditWriter();

        var handler = new ReindexKnowledgeCommandHandler(
            qdrantService.Object,
            indexingService.Object,
            auditWriter.Object,
            CreateCurrentUser().Object);

        var result = await handler.Handle(
            new ReindexKnowledgeCommand(),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Success.Should().BeFalse();
        result.Value.ChunkCount.Should().Be(0);
        result.Value.Message.Should().Contain("Qdrant write timeout");

        auditWriter.Verify(
            x => x.WriteAsync(
                ActorId,
                AdminAuditActions.KnowledgeReindexed,
                "Knowledge",
                null,
                It.IsAny<object?>(),
                AuditSeverity.Error,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
