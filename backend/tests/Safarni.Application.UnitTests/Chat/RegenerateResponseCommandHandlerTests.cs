using ChatBotApi.Application.Common.DTOs;
using ChatBotApi.Application.Common.Interfaces;
using ChatBotApi.Application.Common.Models;
using ChatBotApi.Application.Features.Chat.Commands.RegenerateResponse;
using ChatBotApi.Application.Features.Chat.Services;
using ChatBotApi.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace Safarni.Application.UnitTests.Chat;

public class RegenerateResponseCommandHandlerTests
{
    private static readonly Guid TestUserId = Guid.NewGuid();

    private readonly Mock<IChatRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IAIProvider> _aiProvider = new();
    private readonly Mock<IConfiguration> _configuration = new();
    private readonly Mock<IEmbeddingService> _embeddingService = new();
    private readonly Mock<IQdrantService> _qdrantService = new();
    private readonly Mock<IIntentDetectionService> _intentDetection = new();
    private readonly Mock<IChatScopeClassifier> _scopeClassifier = new();
    private readonly Mock<IGooglePlacesService> _googlePlaces = new();
    private readonly Mock<ILogger<RegenerateResponseCommandHandler>> _logger = new();

    public RegenerateResponseCommandHandlerTests()
    {
        _unitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _repository
            .Setup(x => x.AddMessageAsync(
                It.IsAny<ConversationMessage>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _aiProvider
            .Setup(x => x.GetChatCompletionAsync(
                It.IsAny<string>(),
                It.IsAny<List<ChatMessageDto>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("new generated response");
    }

    [Fact]
    public async Task Handle_ShouldReplaceOnlyMessagesAfterLastUserMessage()
    {
        var conversation = CreateConversationWithTwoTurns();
        var oldResponse = conversation.Messages.Last();
        SetupConversation(conversation);
        _intentDetection
            .Setup(x => x.DetectIntentAsync(
                "شكراً",
                It.IsAny<List<ChatMessageDto>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IntentDetectionResult { Intent = ChatIntent.Social, StandaloneQuery = "شكراً" });
        _scopeClassifier
            .Setup(x => x.ClassifyAsync(
                "شكراً",
                It.IsAny<List<ChatMessageDto>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ChatIntent.Social);
        List<ConversationMessage>? deletedMessages = null;
        _repository
            .Setup(x => x.DeleteMessages(It.IsAny<IEnumerable<ConversationMessage>>()))
            .Callback<IEnumerable<ConversationMessage>>(messages =>
                deletedMessages = messages.ToList());

        var result = await CreateHandler().Handle(
            new RegenerateResponseCommand(conversation.Id, conversation.UserId),
            CancellationToken.None);

        deletedMessages.Should().ContainSingle().Which.Should().BeSameAs(oldResponse);
        result.Messages.Select(message => message.Content).Should().Equal(
            "خطط لي رحلة إلى روما",
            "يمكنك زيارة الكولوسيوم",
            "شكراً",
            "new generated response");
        _repository.Verify(
            x => x.AddMessageAsync(
                It.Is<ConversationMessage>(message =>
                    message.Role == "assistant" &&
                    message.Content == "new generated response"),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
        VerifyTravelLookupsWereSkipped();
    }

    [Fact]
    public async Task Handle_ShouldClassifyAmbiguousLastMessageBeforeRegenerating()
    {
        var conversation = CreateConversationWithTwoTurns();
        conversation.Messages[^2].Content = "عبد الحليم";
        SetupConversation(conversation);
        _intentDetection
            .Setup(x => x.DetectIntentAsync(
                "عبد الحليم",
                It.IsAny<List<ChatMessageDto>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IntentDetectionResult { Intent = ChatIntent.Ambiguous, StandaloneQuery = "عبد الحليم" });
        _scopeClassifier
            .Setup(x => x.ClassifyAsync(
                "عبد الحليم",
                It.IsAny<List<ChatMessageDto>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ChatIntent.OutOfScope);

        await CreateHandler().Handle(
            new RegenerateResponseCommand(conversation.Id, conversation.UserId),
            CancellationToken.None);

        _scopeClassifier.VerifyAll();
        VerifyTravelLookupsWereSkipped();
    }

    [Fact]
    public async Task Handle_ShouldRejectConversationWithoutUserMessages()
    {
        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            UserId = TestUserId,
            Title = "empty",
            Messages =
            [
                Message("assistant", "orphan response", DateTime.UtcNow)
            ]
        };
        SetupConversation(conversation);

        var action = () => CreateHandler().Handle(
            new RegenerateResponseCommand(conversation.Id, conversation.UserId),
            CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
        _repository.Verify(
            x => x.DeleteMessages(It.IsAny<IEnumerable<ConversationMessage>>()),
            Times.Never);
        _aiProvider.Verify(
            x => x.GetChatCompletionAsync(
                It.IsAny<string>(),
                It.IsAny<List<ChatMessageDto>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private RegenerateResponseCommandHandler CreateHandler()
    {
        return new RegenerateResponseCommandHandler(
            _repository.Object,
            _unitOfWork.Object,
            _aiProvider.Object,
            _configuration.Object,
            _embeddingService.Object,
            _qdrantService.Object,
            new PromptBuilder(),
            _intentDetection.Object,
            _scopeClassifier.Object,
            _googlePlaces.Object,
            _logger.Object);
    }

    private void SetupConversation(Conversation conversation)
    {
        _repository
            .Setup(x => x.GetByIdAsync(
                conversation.Id,
                conversation.UserId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);
    }

    private void VerifyTravelLookupsWereSkipped()
    {
        _qdrantService.Verify(x => x.IsHealthyAsync(), Times.Never);
        _embeddingService.Verify(
            x => x.GenerateEmbeddingAsync(It.IsAny<string>()),
            Times.Never);
        _googlePlaces.Verify(
            x => x.TextSearchAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string>()),
            Times.Never);
    }

    private static Conversation CreateConversationWithTwoTurns()
    {
        var start = DateTime.UtcNow.AddMinutes(-5);
        return new Conversation
        {
            Id = Guid.NewGuid(),
            UserId = TestUserId,
            Title = "Rome",
            CreatedAt = start,
            Messages =
            [
                Message("user", "خطط لي رحلة إلى روما", start.AddMinutes(1)),
                Message("assistant", "يمكنك زيارة الكولوسيوم", start.AddMinutes(2)),
                Message("user", "شكراً", start.AddMinutes(3)),
                Message("assistant", "old response", start.AddMinutes(4))
            ]
        };
    }

    private static ConversationMessage Message(
        string role,
        string content,
        DateTime createdAt)
    {
        return new ConversationMessage
        {
            Id = Guid.NewGuid(),
            Role = role,
            Content = content,
            CreatedAt = createdAt
        };
    }
}
