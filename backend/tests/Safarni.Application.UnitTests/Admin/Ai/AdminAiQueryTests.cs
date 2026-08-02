using FluentAssertions;
using Moq;
using Safarni.Application.Abstractions.Auditing;
using Safarni.Application.Abstractions.Authentication;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Results;
using Safarni.Application.Features.Admin.Ai;
using Safarni.Application.Features.Admin.Common;
using Safarni.Domain.Entities;
using Safarni.Domain.Enums;

namespace Safarni.Application.UnitTests.Admin.Ai;

public class AdminAiQueryTests
{
    [Fact]
    public async Task GetConversation_WhenNotFound_ShouldFail()
    {
        var adminRepository = new Mock<IAdminRepository>();
        adminRepository
            .Setup(x => x.GetConversationForAdminAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((AdminConversationData?)null);

        var handler = new GetAiConversationQueryHandler(
            adminRepository.Object,
            new Mock<IAuditLogWriter>().Object,
            new Mock<ICurrentUserService>().Object);

        var result = await handler.Handle(
            new GetAiConversationQuery(Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(AdminErrors.ConversationNotFound.Code);
    }

    [Fact]
    public async Task GetConversation_WhenFound_ShouldAuditAdminView()
    {
        var conversationId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();

        var adminRepository = new Mock<IAdminRepository>();
        adminRepository
            .Setup(x => x.GetConversationForAdminAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AdminConversationData(
                conversationId,
                "Trip to Paris",
                ownerId,
                "owner@example.com",
                "Owner Name",
                DateTime.UtcNow,
                IsPinned: false,
                new[]
                {
                    new AdminConversationMessageData(
                        Guid.NewGuid(),
                        "user",
                        "user message content",
                        DateTime.UtcNow),
                    new AdminConversationMessageData(
                        Guid.NewGuid(),
                        "assistant",
                        "assistant reply content",
                        DateTime.UtcNow)
                }));

        var auditWriter = new Mock<IAuditLogWriter>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(x => x.UserId).Returns(actorId);

        var handler = new GetAiConversationQueryHandler(
            adminRepository.Object,
            auditWriter.Object,
            currentUser.Object);

        var result = await handler.Handle(
            new GetAiConversationQuery(conversationId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Messages.Should().HaveCount(2);
        result.Value.Messages[0].Content.Should().Be("user message content");

        auditWriter.Verify(
            x => x.WriteAsync(
                actorId,
                AdminAuditActions.AIConversationViewedByAdmin,
                "Conversation",
                conversationId.ToString(),
                null,
                It.IsAny<AuditSeverity>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ListActivity_ShouldMapWithoutExposingPromptContent()
    {
        var logId = Guid.NewGuid();
        var adminRepository = new Mock<IAdminRepository>();
        adminRepository
            .Setup(x => x.SearchAiActivityAsync(
                It.IsAny<AiActivityCriteria>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<AIRequestLog>(
                new[]
                {
                    new AIRequestLog
                    {
                        Id = logId,
                        UserId = Guid.NewGuid(),
                        Provider = "Groq",
                        Model = "llama-3.3-70b-versatile",
                        Operation = "Chat.Completion",
                        Status = "Success",
                        LatencyMs = 1234,
                        PromptTokens = 42,
                        CompletionTokens = 10,
                        CreatedAt = DateTime.UtcNow
                    }
                },
                1,
                20,
                1));

        var handler = new ListAiActivityQueryHandler(adminRepository.Object);

        var result = await handler.Handle(
            new ListAiActivityQuery(new AiActivityCriteria()),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var item = result.Value!.Items.Single();
        item.Id.Should().Be(logId);
        item.Provider.Should().Be("Groq");
        item.LatencyMs.Should().Be(1234);
        item.PromptTokens.Should().Be(42);
    }
}
