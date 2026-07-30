using System.Runtime.CompilerServices;
using System.Text;
using ChatBotApi.Application.Common.DTOs;
using ChatBotApi.Application.Common.Interfaces;
using ChatBotApi.Application.Common.Models.GooglePlaces;
using ChatBotApi.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ChatBotApi.Application.Features.Chat.StreamChatResponse;

public sealed record StreamChatResponseCommand(
    Guid ConversationId,
    string UserId,
    string Message)
    : IStreamRequest<ChatStreamEvent>;

public sealed class StreamChatResponseCommandHandler
    : IStreamRequestHandler<
        StreamChatResponseCommand,
        ChatStreamEvent>
{
    private const int MaxHistoryMessages = 8;
    private const int MaxContextChunks = 3;

    private readonly IChatRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAIProvider _aiProvider;
    private readonly IConfiguration _configuration;
    private readonly IEmbeddingService _embeddingService;
    private readonly IQdrantService _qdrantService;
    private readonly IPromptBuilder _promptBuilder;
    private readonly IIntentDetectionService
        _intentDetectionService;
    private readonly IGooglePlacesService
        _googlePlacesService;
    private readonly IValidator<StreamChatResponseCommand>
        _validator;
    private readonly ILogger<StreamChatResponseCommandHandler>
        _logger;

    public StreamChatResponseCommandHandler(
        IChatRepository repository,
        IUnitOfWork unitOfWork,
        IAIProvider aiProvider,
        IConfiguration configuration,
        IEmbeddingService embeddingService,
        IQdrantService qdrantService,
        IPromptBuilder promptBuilder,
        IIntentDetectionService intentDetectionService,
        IGooglePlacesService googlePlacesService,
        IValidator<StreamChatResponseCommand> validator,
        ILogger<StreamChatResponseCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _aiProvider = aiProvider;
        _configuration = configuration;
        _embeddingService = embeddingService;
        _qdrantService = qdrantService;
        _promptBuilder = promptBuilder;
        _intentDetectionService = intentDetectionService;
        _googlePlacesService = googlePlacesService;
        _validator = validator;
        _logger = logger;
    }

    public async IAsyncEnumerable<ChatStreamEvent> Handle(
        StreamChatResponseCommand request,
        [EnumeratorCancellation]
        CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(
            request,
            cancellationToken);

        var conversation =
            await _repository.GetByIdAsync(
                request.ConversationId,
                request.UserId,
                cancellationToken);

        if (conversation is null)
        {
            throw new KeyNotFoundException(
                "The conversation was not found or does not belong to the current user.");
        }

        var history = conversation.Messages
            .OrderBy(message => message.CreatedAt)
            .TakeLast(MaxHistoryMessages)
            .Select(message => new ChatMessageDto
            {
                Role = message.Role,
                Content = message.Content
            })
            .ToList();

        await SaveUserMessageAsync(
            conversation,
            request.Message,
            cancellationToken);

        var responseContext =
            await BuildResponseContextAsync(
                request.Message,
                cancellationToken);

        var completeResponse = new StringBuilder();

        if (responseContext.ImmediateResponse is not null)
        {
            completeResponse.Append(
                responseContext.ImmediateResponse);

            yield return ChatStreamEvent.TokenEvent(
                responseContext.ImmediateResponse);
        }
        else
        {
            await foreach (var token in _aiProvider
                               .StreamChatCompletionAsync(
                                   responseContext.SystemPrompt!,
                                   history,
                                   responseContext.UserPrompt,
                                   cancellationToken)
                               .WithCancellation(
                                   cancellationToken))
            {
                if (string.IsNullOrEmpty(token))
                {
                    continue;
                }

                completeResponse.Append(token);

                yield return ChatStreamEvent.TokenEvent(
                    token);
            }
        }

        var assistantResponse =
            completeResponse.ToString().Trim();

        if (string.IsNullOrWhiteSpace(assistantResponse))
        {
            throw new InvalidOperationException(
                "The AI provider returned an empty response.");
        }

        await SaveAssistantMessageAsync(
            conversation,
            assistantResponse,
            cancellationToken);

        yield return ChatStreamEvent.CompletedEvent(
            MapConversation(conversation));
    }

    private async Task SaveUserMessageAsync(
        Conversation conversation,
        string content,
        CancellationToken cancellationToken)
    {
        var userMessage = new ConversationMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            Role = "user",
            Content = content.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        conversation.Messages.Add(userMessage);

        await _repository.AddMessageAsync(
            userMessage,
            cancellationToken);

        if (string.IsNullOrWhiteSpace(conversation.Title) ||
            string.Equals(
                conversation.Title,
                "New Conversation",
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                conversation.Title,
                "محادثة جديدة",
                StringComparison.Ordinal))
        {
            conversation.Title = BuildConversationTitle(
                content);
        }

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);
    }

    private async Task SaveAssistantMessageAsync(
        Conversation conversation,
        string content,
        CancellationToken cancellationToken)
    {
        var assistantMessage = new ConversationMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            Role = "assistant",
            Content = content,
            CreatedAt = DateTime.UtcNow
        };

        conversation.Messages.Add(assistantMessage);

        await _repository.AddMessageAsync(
            assistantMessage,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);
    }

    private async Task<ChatResponseContext>
        BuildResponseContextAsync(
            string message,
            CancellationToken cancellationToken)
    {
        var intent =
            _intentDetectionService.DetectIntent(message);

        _logger.LogInformation(
            "Detected streaming chat intent: {Intent}",
            intent);

        var contextChunks =
            await FindContextChunksAsync(
                message,
                cancellationToken);

        string? systemPrompt = null;
        string? immediateResponse = null;

        if (contextChunks.Count > 0)
        {
            systemPrompt =
                _promptBuilder.BuildSystemPrompt(
                    contextChunks,
                    message);
        }
        else if (intent == ChatIntent.RagOnly)
        {
            systemPrompt =
                _promptBuilder.BuildSystemPrompt(
                    [],
                    message);
        }
        else
        {
            var places = await FindPlacesAsync(
                message,
                cancellationToken);

            if (places.Count > 0)
            {
                systemPrompt =
                    _promptBuilder.BuildSystemPromptWithPlaces(
                        null,
                        places,
                        intent,
                        message);
            }
            else
            {
                immediateResponse =
                    _promptBuilder
                        .BuildKnowledgeNotFoundMessage(
                            message);
            }
        }

        return new ChatResponseContext(
            systemPrompt,
            _promptBuilder.BuildUserPrompt(message),
            immediateResponse);
    }

    private async Task<List<DocumentChunk>>
        FindContextChunksAsync(
            string message,
            CancellationToken cancellationToken)
    {
        List<DocumentChunk>? contextChunks = null;

        try
        {
            if (await _qdrantService.IsHealthyAsync())
            {
                var queryEmbedding =
                    await _embeddingService
                        .GenerateEmbeddingAsync(message);

                var configuredTopK = int.TryParse(
                    _configuration["RAG:TopK"],
                    out var topK)
                        ? topK
                        : MaxContextChunks;

                contextChunks =
                    await _qdrantService
                        .SearchSimilarChunksAsync(
                            queryEmbedding,
                            Math.Min(
                                configuredTopK,
                                MaxContextChunks));
            }
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Vector search failed while preparing a streaming chat response. Using SQL fallback.");
        }

        if (contextChunks is null ||
            contextChunks.Count == 0)
        {
            contextChunks =
                await GetSqlFallbackChunksAsync(
                    message,
                    cancellationToken);
        }

        return contextChunks
            .Take(MaxContextChunks)
            .ToList();
    }

    private async Task<List<PlaceDetails>> FindPlacesAsync(
        string message,
        CancellationToken cancellationToken)
    {
        try
        {
            var places =
                await _googlePlacesService.TextSearchAsync(
                    message,
                    cancellationToken,
                    _promptBuilder
                        .GetGooglePlacesLanguageCode(
                            message));

            return places ?? [];
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Google Places search failed while preparing a streaming chat response.");

            return [];
        }
    }

    private async Task<List<DocumentChunk>>
        GetSqlFallbackChunksAsync(
            string userMessage,
            CancellationToken cancellationToken)
    {
        var chunks = new List<DocumentChunk>();
        var query = userMessage.ToLowerInvariant();

        var knowledgeItems =
            await _repository.GetTravelKnowledgesAsync(
                cancellationToken);

        foreach (var knowledge in knowledgeItems)
        {
            var locationMatches =
                ContainsLocation(
                    query,
                    knowledge.City) ||
                ContainsLocation(
                    query,
                    knowledge.Country);

            var visaMatches = string.Equals(
                    knowledge.Category,
                    "VisaInfo",
                    StringComparison.OrdinalIgnoreCase) &&
                ContainsVisaTerm(query);

            if (!locationMatches && !visaMatches)
            {
                continue;
            }

            chunks.Add(new DocumentChunk
            {
                Id = knowledge.Id.ToString(),
                EntityId = knowledge.Id.ToString(),
                EntityType = knowledge.Category,
                Content =
                    $"Category: {knowledge.Category}. " +
                    $"Title: {knowledge.Title}. " +
                    $"Location: {knowledge.City}, {knowledge.Country}. " +
                    $"Content: {knowledge.Content}",
                City = knowledge.City ?? string.Empty,
                Country = knowledge.Country ?? string.Empty,
                Source = "SQL Server Database"
            });
        }

        return chunks;
    }

    private static bool ContainsLocation(
        string normalizedQuery,
        string? location)
    {
        if (string.IsNullOrWhiteSpace(location))
        {
            return false;
        }

        return location
            .ToLowerInvariant()
            .Split('/')
            .Select(part => part.Trim())
            .Any(part =>
                part.Length >= 3 &&
                normalizedQuery.Contains(
                    part,
                    StringComparison.Ordinal));
    }

    private static bool ContainsVisaTerm(string query)
    {
        return query.Contains(
                   "schengen",
                   StringComparison.Ordinal) ||
               query.Contains(
                   "شنغن",
                   StringComparison.Ordinal) ||
               query.Contains(
                   "فيزا",
                   StringComparison.Ordinal) ||
               query.Contains(
                   "تأشيرة",
                   StringComparison.Ordinal) ||
               query.Contains(
                   "تاشيره",
                   StringComparison.Ordinal);
    }

    private static string BuildConversationTitle(
        string message)
    {
        var cleanMessage = message.Trim();

        return cleanMessage.Length <= 25
            ? cleanMessage
            : $"{cleanMessage[..22]}...";
    }

    private static ConversationDto MapConversation(
        Conversation conversation)
    {
        return new ConversationDto
        {
            Id = conversation.Id,
            Title = conversation.Title,
            CreatedAt = conversation.CreatedAt,
            Messages = conversation.Messages
                .OrderBy(message => message.CreatedAt)
                .Select(message => new ChatMessageDto
                {
                    Role = message.Role,
                    Content = message.Content
                })
                .ToList()
        };
    }

    private sealed record ChatResponseContext(
        string? SystemPrompt,
        string UserPrompt,
        string? ImmediateResponse);
}
