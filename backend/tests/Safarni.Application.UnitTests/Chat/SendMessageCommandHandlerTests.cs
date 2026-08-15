using ChatBotApi.Application.Common.Interfaces;
using ChatBotApi.Application.Common.Models;
using ChatBotApi.Application.Common.Models.GooglePlaces;
using ChatBotApi.Application.Features.Chat.Commands.SendMessage;
using ChatBotApi.Application.Features.Chat.Services;
using ChatBotApi.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace Safarni.Application.UnitTests.Chat;

public class SendMessageCommandHandlerTests
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
    private readonly Mock<ILogger<SendMessageCommandHandler>> _logger = new();

    public SendMessageCommandHandlerTests()
    {
        _unitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _repository
            .Setup(x => x.AddMessageAsync(
                It.IsAny<ConversationMessage>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _repository
            .Setup(x => x.GetTravelKnowledgesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TravelKnowledge>());
        _qdrantService
            .Setup(x => x.IsHealthyAsync())
            .ReturnsAsync(false);
        _aiProvider
            .Setup(x => x.GetChatCompletionAsync(
                It.IsAny<string>(),
                It.IsAny<List<ChatBotApi.Application.Common.DTOs.ChatMessageDto>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("generated response");
    }

    [Fact]
    public async Task Handle_ShouldGenerateSocialReplyAndSkipAllTravelLookups()
    {
        var conversation = CreateConversation();
        SetupConversation(conversation);
        _intentDetection
            .Setup(x => x.DetectIntentAsync(
                "عامل إيه؟",
                It.IsAny<List<ChatBotApi.Application.Common.DTOs.ChatMessageDto>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IntentDetectionResult { Intent = ChatIntent.Social, StandaloneQuery = "عامل إيه؟" });
        _scopeClassifier
            .Setup(x => x.ClassifyAsync(
                "عامل إيه؟",
                It.IsAny<List<ChatBotApi.Application.Common.DTOs.ChatMessageDto>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ChatIntent.Social);

        var result = await CreateHandler().Handle(
            new SendMessageCommand(conversation.Id, conversation.UserId, "عامل إيه؟"),
            CancellationToken.None);

        result.Messages.Select(message => message.Content)
            .Should().Equal("عامل إيه؟", "generated response");
        _scopeClassifier.Verify(
            x => x.ClassifyAsync(
                "عامل إيه؟",
                It.IsAny<List<ChatBotApi.Application.Common.DTOs.ChatMessageDto>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        VerifyTravelLookupsWereSkipped();
        VerifySuccessfulPersistence();
    }

    [Fact]
    public async Task Handle_ShouldClassifyAmbiguousPersonAndSkipTravelLookupsWhenOutOfScope()
    {
        var conversation = CreateConversation();
        SetupConversation(conversation);
        _intentDetection
            .Setup(x => x.DetectIntentAsync(
                "عبد الحليم",
                It.IsAny<List<ChatBotApi.Application.Common.DTOs.ChatMessageDto>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IntentDetectionResult { Intent = ChatIntent.Ambiguous, StandaloneQuery = "عبد الحليم" });
        _scopeClassifier
            .Setup(x => x.ClassifyAsync(
                "عبد الحليم",
                It.IsAny<List<ChatBotApi.Application.Common.DTOs.ChatMessageDto>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ChatIntent.OutOfScope);

        await CreateHandler().Handle(
            new SendMessageCommand(conversation.Id, conversation.UserId, "عبد الحليم"),
            CancellationToken.None);

        _scopeClassifier.VerifyAll();
        VerifyTravelLookupsWereSkipped();
        VerifySuccessfulPersistence();
    }

    [Fact]
    public async Task Handle_ShouldContinueTravelPipelineWhenSemanticClassifierReturnsTravel()
    {
        var conversation = CreateConversation();
        SetupConversation(conversation);
        _intentDetection
            .Setup(x => x.DetectIntentAsync(
                "Kotor",
                It.IsAny<List<ChatBotApi.Application.Common.DTOs.ChatMessageDto>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IntentDetectionResult { Intent = ChatIntent.Ambiguous, StandaloneQuery = "Kotor" });
        _scopeClassifier
            .Setup(x => x.ClassifyAsync(
                "Kotor",
                It.IsAny<List<ChatBotApi.Application.Common.DTOs.ChatMessageDto>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ChatIntent.RagOnly);

        var result = await CreateHandler().Handle(
            new SendMessageCommand(conversation.Id, conversation.UserId, "Kotor"),
            CancellationToken.None);

        result.Messages.Last().Content.Should().Be("generated response");
        _qdrantService.Verify(x => x.IsHealthyAsync(), Times.Once);
        _repository.Verify(
            x => x.GetTravelKnowledgesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
        _googlePlaces.Verify(
            x => x.TextSearchAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string>()),
            Times.Never);
        VerifySuccessfulPersistence();
    }

    [Fact]
    public async Task Handle_ShouldLetSemanticGateRejectMessageEvenWhenKeywordsLookSocialOrTravelRelated()
    {
        var conversation = CreateConversation();
        SetupConversation(conversation);
        const string message = "hello, tell me about a singer who loves travel";
        _intentDetection
            .Setup(x => x.DetectIntentAsync(
                message,
                It.IsAny<List<ChatBotApi.Application.Common.DTOs.ChatMessageDto>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IntentDetectionResult { Intent = ChatIntent.RagOnly, StandaloneQuery = message });
        _scopeClassifier
            .Setup(x => x.ClassifyAsync(
                message,
                It.IsAny<List<ChatBotApi.Application.Common.DTOs.ChatMessageDto>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ChatIntent.OutOfScope);

        var result = await CreateHandler().Handle(
            new SendMessageCommand(conversation.Id, conversation.UserId, message),
            CancellationToken.None);

        result.Messages.Last().Content.Should().Be(
            "عذراً، أنا مساعد Safarni السياحي، ويمكنني مساعدتك فقط في كل ما يتعلق بالسفر والسياحة. يرجى طرح سؤال متعلق بالسياحة.");
        _aiProvider.Verify(
            x => x.GetChatCompletionAsync(
                It.IsAny<string>(),
                It.IsAny<List<ChatBotApi.Application.Common.DTOs.ChatMessageDto>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        VerifyTravelLookupsWereSkipped();
        VerifySuccessfulPersistence();
    }

    [Fact]
    public async Task Handle_ShouldPreservePlacesRouteOnlyAfterSemanticTravelApproval()
    {
        var conversation = CreateConversation();
        SetupConversation(conversation);
        const string message = "recommend hotels in Kotor";
        _intentDetection
            .Setup(x => x.DetectIntentAsync(
                message,
                It.IsAny<List<ChatBotApi.Application.Common.DTOs.ChatMessageDto>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IntentDetectionResult { Intent = ChatIntent.GooglePlacesOnly, StandaloneQuery = message });
        _scopeClassifier
            .Setup(x => x.ClassifyAsync(
                message,
                It.IsAny<List<ChatBotApi.Application.Common.DTOs.ChatMessageDto>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ChatIntent.RagOnly);
        _googlePlaces
            .Setup(x => x.TextSearchAsync(
                message,
                It.IsAny<CancellationToken>(),
                "en"))
            .ReturnsAsync(new List<PlaceDetails>());

        await CreateHandler().Handle(
            new SendMessageCommand(conversation.Id, conversation.UserId, message),
            CancellationToken.None);

        _googlePlaces.Verify(
            x => x.TextSearchAsync(
                message,
                It.IsAny<CancellationToken>(),
                "en"),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldSkipSemanticClassifierForClearlyOutOfScopeMessage()
    {
        var conversation = CreateConversation();
        SetupConversation(conversation);
        const string message = "اكتب لي كود بايثون";
        _intentDetection
            .Setup(x => x.DetectIntentAsync(
                message,
                It.IsAny<List<ChatBotApi.Application.Common.DTOs.ChatMessageDto>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IntentDetectionResult { Intent = ChatIntent.OutOfScope, StandaloneQuery = message });

        await CreateHandler().Handle(
            new SendMessageCommand(conversation.Id, conversation.UserId, message),
            CancellationToken.None);

        _scopeClassifier.Verify(
            x => x.ClassifyAsync(
                It.IsAny<string>(),
                It.IsAny<List<ChatBotApi.Application.Common.DTOs.ChatMessageDto>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        VerifyTravelLookupsWereSkipped();
    }

    [Fact]
    public async Task Handle_ShouldPropagateCancellationWithoutFallingBackOrCallingModel()
    {
        var conversation = CreateConversation();
        SetupConversation(conversation);
        const string message = "خطط لي رحلة إلى باريس";
        _intentDetection
            .Setup(x => x.DetectIntentAsync(
                message,
                It.IsAny<List<ChatBotApi.Application.Common.DTOs.ChatMessageDto>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IntentDetectionResult { Intent = ChatIntent.RagOnly, StandaloneQuery = message });
        _scopeClassifier
            .Setup(x => x.ClassifyAsync(
                message,
                It.IsAny<List<ChatBotApi.Application.Common.DTOs.ChatMessageDto>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ChatIntent.RagOnly);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        _qdrantService
            .Setup(x => x.IsHealthyAsync())
            .ThrowsAsync(new OperationCanceledException(cancellation.Token));

        var action = () => CreateHandler().Handle(
            new SendMessageCommand(conversation.Id, conversation.UserId, message),
            cancellation.Token);

        await action.Should().ThrowAsync<OperationCanceledException>();
        _repository.Verify(
            x => x.GetTravelKnowledgesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
        _aiProvider.Verify(
            x => x.GetChatCompletionAsync(
                It.IsAny<string>(),
                It.IsAny<List<ChatBotApi.Application.Common.DTOs.ChatMessageDto>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldFailBeforeCallingDependenciesWhenConversationIsMissing()
    {
        var conversationId = Guid.NewGuid();
        _repository
            .Setup(x => x.GetByIdAsync(
                conversationId,
                TestUserId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Conversation?)null);

        var action = () => CreateHandler().Handle(
            new SendMessageCommand(conversationId, TestUserId, "hello"),
            CancellationToken.None);

        await action.Should().ThrowAsync<KeyNotFoundException>();
        _aiProvider.Verify(
            x => x.GetChatCompletionAsync(
                It.IsAny<string>(),
                It.IsAny<List<ChatBotApi.Application.Common.DTOs.ChatMessageDto>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private SendMessageCommandHandler CreateHandler()
    {
        return new SendMessageCommandHandler(
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
        _repository.Verify(
            x => x.GetTravelKnowledgesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
        _googlePlaces.Verify(
            x => x.TextSearchAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string>()),
            Times.Never);
    }

    private void VerifySuccessfulPersistence()
    {
        _repository.Verify(
            x => x.AddMessageAsync(
                It.IsAny<ConversationMessage>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static Conversation CreateConversation()
    {
        return new Conversation
        {
            Id = Guid.NewGuid(),
            UserId = TestUserId,
            Title = "محادثة جديدة",
            CreatedAt = DateTime.UtcNow
        };
    }
}
