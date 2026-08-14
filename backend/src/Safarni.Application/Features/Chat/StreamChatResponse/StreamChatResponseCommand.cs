using System.Runtime.CompilerServices;
using System.Text;
using System.Diagnostics;
using ChatBotApi.Application.Common.DTOs;
using ChatBotApi.Application.Common.Interfaces;
using ChatBotApi.Application.Common.Models.GooglePlaces;
using ChatBotApi.Application.Features.Chat.Services;
using ChatBotApi.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Safarni.Application.Abstractions.AI;

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
    private const int MaxHistoryMessages = 12;
    private const int MaxContextChunks = 6;

    private readonly IChatRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAIProvider _aiProvider;
    private readonly IConfiguration _configuration;
    private readonly IEmbeddingService _embeddingService;
    private readonly IQdrantService _qdrantService;
    private readonly IPromptBuilder _promptBuilder;
    private readonly IIntentDetectionService
        _intentDetectionService;
    private readonly IChatScopeClassifier
        _scopeClassifier;
    private readonly IGooglePlacesService
        _googlePlacesService;
    private readonly IValidator<StreamChatResponseCommand>
        _validator;
    private readonly ILogger<StreamChatResponseCommandHandler>
        _logger;
    private readonly IAIRequestLogWriter? _telemetryWriter;

    public StreamChatResponseCommandHandler(
        IChatRepository repository,
        IUnitOfWork unitOfWork,
        IAIProvider aiProvider,
        IConfiguration configuration,
        IEmbeddingService embeddingService,
        IQdrantService qdrantService,
        IPromptBuilder promptBuilder,
        IIntentDetectionService intentDetectionService,
        IChatScopeClassifier scopeClassifier,
        IGooglePlacesService googlePlacesService,
        IValidator<StreamChatResponseCommand> validator,
        ILogger<StreamChatResponseCommandHandler> logger,
        IAIRequestLogWriter? telemetryWriter = null)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _aiProvider = aiProvider;
        _configuration = configuration;
        _embeddingService = embeddingService;
        _qdrantService = qdrantService;
        _promptBuilder = promptBuilder;
        _intentDetectionService = intentDetectionService;
        _scopeClassifier = scopeClassifier;
        _googlePlacesService = googlePlacesService;
        _validator = validator;
        _logger = logger;
        _telemetryWriter = telemetryWriter;
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

        var isArabic = IsArabic(request.Message);
        yield return ChatStreamEvent.StatusEvent(
            isArabic
                ? "أفهم طلبك وسياق الرحلة"
                : "Understanding your trip context");

        var intentResult = await ResolveIntentAsync(
            request.Message,
            history,
            cancellationToken);
        var intent = intentResult.Intent;
        ChatResponseContext responseContext;

        if (intent is ChatIntent.Social or ChatIntent.OutOfScope)
        {
            responseContext = new ChatResponseContext(
                _promptBuilder.BuildDirectSystemPrompt(intent, request.Message),
                _promptBuilder.BuildUserPrompt(request.Message),
                ImmediateResponse: null);
        }
        else
        {
            var clarification = TripPlanningClarifier.BuildClarification(
                intentResult.IsTripPlanningRequest,
                request.Message,
                history);

            if (!string.IsNullOrWhiteSpace(clarification))
            {
                responseContext = new ChatResponseContext(
                    SystemPrompt: null,
                    UserPrompt: request.Message,
                    ImmediateResponse: clarification);
            }
            else
            {
                yield return ChatStreamEvent.StatusEvent(
                    isArabic
                        ? "أراجع معرفة سفر موثوقة"
                        : "Reviewing trusted travel knowledge");

                var contextChunks = await FindContextChunksAsync(
                    intentResult.StandaloneQuery,
                    cancellationToken);
                var places = new List<PlaceDetails>();

                if (intent is ChatIntent.GooglePlacesOnly or ChatIntent.Hybrid)
                {
                    yield return ChatStreamEvent.StatusEvent(
                        isArabic
                            ? "أبحث عن أماكن مناسبة"
                            : "Finding places that fit");

                    places = await FindPlacesAsync(
                        intentResult.StandaloneQuery,
                        cancellationToken);
                }

                var resources = BuildResources(contextChunks, places);
                if (resources.Sources.Count > 0 || resources.Places.Count > 0)
                {
                    yield return ChatStreamEvent.ResourcesEvent(
                        resources.Sources,
                        resources.Places);
                }

                var systemPrompt = intent is ChatIntent.GooglePlacesOnly or ChatIntent.Hybrid
                    ? _promptBuilder.BuildSystemPromptWithPlaces(
                        contextChunks,
                        places,
                        intent,
                        request.Message)
                    : _promptBuilder.BuildSystemPrompt(
                        contextChunks,
                        request.Message);

                responseContext = new ChatResponseContext(
                    systemPrompt,
                    _promptBuilder.BuildUserPrompt(request.Message),
                    ImmediateResponse: null);
            }
        }

        yield return ChatStreamEvent.StatusEvent(
            isArabic
                ? "أجهّز لك ردًا مناسبًا"
                : "Putting together a useful answer");

        var completeResponse = new StringBuilder();
        var stopwatch = Stopwatch.StartNew();
        var streamStatus = "Success";
        string? streamErrorCode = null;

        try
        {
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
                streamStatus = "Failure";
                streamErrorCode = "EmptyResponse";

                throw new InvalidOperationException(
                    "The AI provider returned an empty response.");
            }
        }
        finally
        {
            stopwatch.Stop();

            await AIRequestTelemetry.RecordAsync(
                _telemetryWriter,
                new AIRequestLogEntry(
                    Guid.TryParse(
                        request.UserId,
                        out var parsedUserId)
                        ? parsedUserId
                        : null,
                    request.ConversationId,
                    ItineraryJobId: null,
                    _aiProvider.ProviderName,
                    _aiProvider.ModelName,
                    "Chat.StreamCompletion",
                    streamStatus,
                    stopwatch.ElapsedMilliseconds,
                    ErrorCode: streamErrorCode),
                cancellationToken);
        }

        await SaveAssistantMessageAsync(
            conversation,
            completeResponse.ToString().Trim(),
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

    private async Task<IntentDetectionResult> ResolveIntentAsync(
        string message,
        List<ChatMessageDto> history,
        CancellationToken cancellationToken)
    {
        var result = await _intentDetectionService.DetectIntentAsync(
            message,
            history,
            cancellationToken) ?? new IntentDetectionResult
            {
                Intent = _intentDetectionService.DetectIntent(message),
                StandaloneQuery = message
            };

        if (result.Intent == ChatIntent.OutOfScope ||
            !result.RequiresSemanticClassification)
        {
            return result;
        }

        var semanticIntent =
            await _scopeClassifier.ClassifyAsync(
                message,
                history,
                cancellationToken);

        if (semanticIntent is ChatIntent.OutOfScope or
            ChatIntent.Social)
        {
            result.Intent = semanticIntent;
            return result;
        }

        result.Intent = result.Intent switch
        {
            ChatIntent.GooglePlacesOnly =>
                ChatIntent.GooglePlacesOnly,
            ChatIntent.Hybrid =>
                ChatIntent.Hybrid,
            _ =>
                ChatIntent.RagOnly
        };

        return result;
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
                Source = "SQL Server Database",
                SourceUrl = knowledge.SourceUrl
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

    private static ChatResources BuildResources(
        IEnumerable<DocumentChunk> contextChunks,
        IEnumerable<PlaceDetails> places)
    {
        var sources = new List<ChatSource>();
        var seenUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var chunk in contextChunks)
        {
            if (!TryGetHttpDomain(chunk.SourceUrl, out var sourceUrl, out var domain) ||
                !seenUrls.Add(sourceUrl))
            {
                continue;
            }

            var location = !string.IsNullOrWhiteSpace(chunk.City)
                ? chunk.City
                : chunk.Country;
            var title = string.IsNullOrWhiteSpace(location)
                ? $"Safarni {chunk.EntityType} source"
                : $"{location} · {chunk.EntityType}";

            sources.Add(new ChatSource(
                title,
                sourceUrl,
                domain,
                "knowledge"));
        }

        var placeCards = new List<ChatPlaceCard>();
        foreach (var place in places.Take(5))
        {
            if (string.IsNullOrWhiteSpace(place.GoogleMapsLink))
            {
                continue;
            }

            placeCards.Add(new ChatPlaceCard(
                place.Name,
                place.Address,
                place.Rating,
                place.UserRatingsTotal,
                place.GoogleMapsLink,
                place.Description));

            if (TryGetHttpDomain(place.GoogleMapsLink, out var mapUrl, out var mapDomain) &&
                seenUrls.Add(mapUrl))
            {
                sources.Add(new ChatSource(
                    place.Name,
                    mapUrl,
                    mapDomain,
                    "place"));
            }
        }

        return new ChatResources(
            sources.Take(8).ToList(),
            placeCards);
    }

    private static bool TryGetHttpDomain(
        string? value,
        out string url,
        out string domain)
    {
        url = string.Empty;
        domain = string.Empty;

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("http" or "https"))
        {
            return false;
        }

        url = uri.AbsoluteUri;
        domain = uri.Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase)
            ? uri.Host[4..]
            : uri.Host;
        return true;
    }

    private static bool IsArabic(string text)
    {
        return text.Any(character => character is >= '\u0600' and <= '\u06FF');
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

    private sealed record ChatResources(
        IReadOnlyList<ChatSource> Sources,
        IReadOnlyList<ChatPlaceCard> Places);
}
