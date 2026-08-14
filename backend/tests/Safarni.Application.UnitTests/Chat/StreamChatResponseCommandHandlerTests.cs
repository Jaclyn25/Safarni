using System.Runtime.CompilerServices;
using ChatBotApi.Application.Common.DTOs;
using ChatBotApi.Application.Common.Interfaces;
using ChatBotApi.Application.Common.Models.GooglePlaces;
using ChatBotApi.Application.Features.Chat.StreamChatResponse;
using ChatBotApi.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Safarni.Application.UnitTests.Chat;

public sealed class
    StreamChatResponseCommandHandlerTests
{
    [Fact]
    public async Task Handle_Should_StreamTokens_ThenSaveCompletedResponse()
    {
        var fixture = CreateFixture();

        fixture.AiProvider
            .Setup(provider =>
                provider.StreamChatCompletionAsync(
                    It.IsAny<string>(),
                    It.IsAny<List<ChatMessageDto>>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(
                StreamTokensAsync(
                    "Hello",
                    " traveler"));

        var events = new List<ChatStreamEvent>();

        await foreach (var streamEvent in
                       fixture.Handler.Handle(
                           fixture.Command,
                           CancellationToken.None))
        {
            events.Add(streamEvent);
        }

        events
            .Select(streamEvent => streamEvent.Type)
            .Should()
            .ContainInOrder(
                "token",
                "token",
                "completed");

        events
            .Where(streamEvent =>
                streamEvent.Type == "token")
            .Select(streamEvent =>
                streamEvent.Token)
            .Should()
            .ContainInOrder(
                "Hello",
                " traveler");

        events[^1].Conversation
            .Should().NotBeNull();

        fixture.Conversation.Messages
            .Should().HaveCount(2);

        fixture.Conversation.Messages
            .Last()
            .Content
            .Should().Be("Hello traveler");

        fixture.ChatRepository.Verify(
            repository =>
                repository.AddMessageAsync(
                    It.IsAny<ConversationMessage>(),
                    It.IsAny<CancellationToken>()),
            Times.Exactly(2));

        fixture.UnitOfWork.Verify(
            unitOfWork =>
                unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_Should_NotSavePartialAssistantMessage_WhenCancelled()
    {
        var fixture = CreateFixture();
        using var cancellation =
            new CancellationTokenSource();

        fixture.AiProvider
            .Setup(provider =>
                provider.StreamChatCompletionAsync(
                    It.IsAny<string>(),
                    It.IsAny<List<ChatMessageDto>>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns((
                string _,
                List<ChatMessageDto> _,
                string _,
                CancellationToken cancellationToken) =>
                    StreamUntilCancelledAsync(
                        cancellationToken));

        await using var enumerator =
            fixture.Handler
                .Handle(
                    fixture.Command,
                    cancellation.Token)
                .GetAsyncEnumerator(
                    cancellation.Token);

        do
        {
            (await enumerator.MoveNextAsync())
                .Should().BeTrue();
        }
        while (enumerator.Current.Type != "token");

        enumerator.Current.Type
            .Should().Be("token");

        enumerator.Current.Token
            .Should().Be("Partial");

        cancellation.Cancel();

        Func<Task> continueStream = async () =>
        {
            await enumerator.MoveNextAsync();
        };

        await continueStream
            .Should()
            .ThrowAsync<OperationCanceledException>();

        fixture.Conversation.Messages
            .Should().ContainSingle();

        fixture.Conversation.Messages
            .Single()
            .Role
            .Should().Be("user");

        fixture.ChatRepository.Verify(
            repository =>
                repository.AddMessageAsync(
                    It.Is<ConversationMessage>(
                        message =>
                            message.Role ==
                            "assistant"),
                    It.IsAny<CancellationToken>()),
            Times.Never);

        fixture.UnitOfWork.Verify(
            unitOfWork =>
                unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_HybridRequest_ShouldCombineRagAndGooglePlaces()
    {
        var fixture = CreateFixture();
        var chunk = new DocumentChunk
        {
            Id = Guid.NewGuid().ToString(),
            EntityId = "1",
            EntityType = "City",
            Content = "Curated Istanbul context",
            SourceUrl = "https://example.com/istanbul-guide",
            Score = 0.82f
        };
        var place = new PlaceDetails
        {
            PlaceId = "place-1",
            Name = "Hagia Sophia",
            Address = "Istanbul",
            Rating = 4.8,
            UserRatingsTotal = 100,
            GoogleMapsLink = "https://maps.google.com/?q=Hagia+Sophia"
        };

        fixture.IntentDetectionService
            .Setup(service => service.DetectIntentAsync(
                It.IsAny<string>(),
                It.IsAny<List<ChatMessageDto>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IntentDetectionResult
            {
                Intent = ChatIntent.Hybrid,
                StandaloneQuery = "10 day Istanbul itinerary"
            });
        fixture.QdrantService.Setup(service => service.IsHealthyAsync()).ReturnsAsync(true);
        fixture.EmbeddingService
            .Setup(service => service.GenerateEmbeddingAsync("10 day Istanbul itinerary", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new float[2048]);
        fixture.QdrantService
            .Setup(service => service.SearchSimilarChunksAsync(It.IsAny<float[]>(), It.IsAny<int>()))
            .ReturnsAsync([chunk]);
        fixture.GooglePlacesService
            .Setup(service => service.TextSearchAsync(
                "10 day Istanbul itinerary",
                It.IsAny<CancellationToken>(),
                It.IsAny<string>()))
            .ReturnsAsync([place]);
        fixture.PromptBuilder
            .Setup(builder => builder.BuildSystemPromptWithPlaces(
                It.Is<List<DocumentChunk>>(chunks => chunks.Contains(chunk)),
                It.Is<List<PlaceDetails>>(places => places.Contains(place)),
                ChatIntent.Hybrid,
                It.IsAny<string>()))
            .Returns("Hybrid grounded prompt");
        fixture.AiProvider
            .Setup(provider => provider.StreamChatCompletionAsync(
                "Hybrid grounded prompt",
                It.IsAny<List<ChatMessageDto>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(StreamTokensAsync("Grounded answer"));

        var events = new List<ChatStreamEvent>();
        await foreach (var streamEvent in fixture.Handler.Handle(fixture.Command, CancellationToken.None))
        {
            events.Add(streamEvent);
        }

        events.Should().Contain(streamEvent =>
            streamEvent.Type == "status" &&
            streamEvent.Status == "Finding places that fit");
        events.Should().Contain(streamEvent =>
            streamEvent.Type == "resources" &&
            streamEvent.Sources!.Any(source => source.Domain == "example.com") &&
            streamEvent.Places!.Any(card => card.Name == "Hagia Sophia" && card.Rating == 4.8));

        fixture.PromptBuilder.Verify(builder => builder.BuildSystemPromptWithPlaces(
            It.Is<List<DocumentChunk>>(chunks => chunks.Contains(chunk)),
            It.Is<List<PlaceDetails>>(places => places.Contains(place)),
            ChatIntent.Hybrid,
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Handle_BroadTripRequest_ShouldAskPreferencesBeforeRetrievalOrGeneration()
    {
        var fixture = CreateFixture();
        fixture.IntentDetectionService
            .Setup(service => service.DetectIntentAsync(
                It.IsAny<string>(),
                It.IsAny<List<ChatMessageDto>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IntentDetectionResult
            {
                Intent = ChatIntent.Hybrid,
                StandaloneQuery = "plan a trip",
                IsTripPlanningRequest = true,
                RequiresSemanticClassification = false
            });

        var events = new List<ChatStreamEvent>();
        await foreach (var streamEvent in fixture.Handler.Handle(fixture.Command, CancellationToken.None))
        {
            events.Add(streamEvent);
        }

        events.Should().Contain(streamEvent =>
            streamEvent.Type == "token" &&
            streamEvent.Token!.Contains("where would you like to go?"));
        fixture.AiProvider.Verify(provider => provider.StreamChatCompletionAsync(
            It.IsAny<string>(),
            It.IsAny<List<ChatMessageDto>>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
        fixture.QdrantService.Verify(service => service.IsHealthyAsync(), Times.Never);
        fixture.GooglePlacesService.Verify(service => service.TextSearchAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<string>()), Times.Never);
    }

    private static TestFixture CreateFixture()
    {
        const string userId = "traveler-123";
        const string userMessage =
            "Help me plan a balanced trip.";

        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = "Travel planning",
            CreatedAt = DateTime.UtcNow
        };

        var chatRepository =
            new Mock<IChatRepository>();

        var unitOfWork =
            new Mock<IUnitOfWork>();

        var aiProvider =
            new Mock<IAIProvider>();

        var embeddingService =
            new Mock<IEmbeddingService>();

        var qdrantService =
            new Mock<IQdrantService>();

        var promptBuilder =
            new Mock<IPromptBuilder>();

        var intentDetectionService =
            new Mock<IIntentDetectionService>();

        var scopeClassifier =
            new Mock<IChatScopeClassifier>();

        var googlePlacesService =
            new Mock<IGooglePlacesService>();

        var logger =
            new Mock<
                ILogger<
                    StreamChatResponseCommandHandler>>();

        chatRepository
            .Setup(repository =>
                repository.GetByIdAsync(
                    conversation.Id,
                    userId,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        chatRepository
            .Setup(repository =>
                repository.AddMessageAsync(
                    It.IsAny<ConversationMessage>(),
                    It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        chatRepository
            .Setup(repository =>
                repository.GetTravelKnowledgesAsync(
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        unitOfWork
            .Setup(unit =>
                unit.SaveChangesAsync(
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        qdrantService
            .Setup(service =>
                service.IsHealthyAsync())
            .ReturnsAsync(false);

        intentDetectionService
            .Setup(service =>
                service.DetectIntent(userMessage))
            .Returns(ChatIntent.RagOnly);

        scopeClassifier
            .Setup(service =>
                service.ClassifyAsync(
                    userMessage,
                    It.IsAny<List<ChatMessageDto>>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(ChatIntent.RagOnly);

        promptBuilder
            .Setup(builder =>
                builder.BuildSystemPrompt(
                    It.IsAny<List<DocumentChunk>>(),
                    userMessage))
            .Returns("System prompt");

        promptBuilder
            .Setup(builder =>
                builder.BuildUserPrompt(userMessage))
            .Returns(userMessage);

        var configuration =
            new ConfigurationBuilder().Build();

        var validator =
            new StreamChatResponseCommandValidator();

        var handler =
            new StreamChatResponseCommandHandler(
                chatRepository.Object,
                unitOfWork.Object,
                aiProvider.Object,
                configuration,
                embeddingService.Object,
                qdrantService.Object,
                promptBuilder.Object,
                intentDetectionService.Object,
                scopeClassifier.Object,
                googlePlacesService.Object,
                validator,
                logger.Object);

        return new TestFixture(
            handler,
            new StreamChatResponseCommand(
                conversation.Id,
                userId,
                userMessage),
            conversation,
            chatRepository,
            unitOfWork,
            aiProvider,
            embeddingService,
            qdrantService,
            promptBuilder,
            intentDetectionService,
            googlePlacesService);
    }

    private static async IAsyncEnumerable<string>
        StreamTokensAsync(params string[] tokens)
    {
        foreach (var token in tokens)
        {
            await Task.Yield();
            yield return token;
        }
    }

    private static async IAsyncEnumerable<string>
        StreamUntilCancelledAsync(
            [EnumeratorCancellation]
            CancellationToken cancellationToken)
    {
        yield return "Partial";

        await Task.Delay(
            Timeout.InfiniteTimeSpan,
            cancellationToken);
    }

    private sealed record TestFixture(
        StreamChatResponseCommandHandler Handler,
        StreamChatResponseCommand Command,
        Conversation Conversation,
        Mock<IChatRepository> ChatRepository,
        Mock<IUnitOfWork> UnitOfWork,
        Mock<IAIProvider> AiProvider,
        Mock<IEmbeddingService> EmbeddingService,
        Mock<IQdrantService> QdrantService,
        Mock<IPromptBuilder> PromptBuilder,
        Mock<IIntentDetectionService> IntentDetectionService,
        Mock<IGooglePlacesService> GooglePlacesService);
}
