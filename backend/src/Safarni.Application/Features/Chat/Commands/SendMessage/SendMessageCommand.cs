using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ChatBotApi.Application.Common.DTOs;
using ChatBotApi.Application.Common.Interfaces;
using ChatBotApi.Application.Common.Models.GooglePlaces;
using ChatBotApi.Application.Features.Chat.Services;
using ChatBotApi.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Safarni.Application.Abstractions.AI;

namespace ChatBotApi.Application.Features.Chat.Commands.SendMessage;

public record SendMessageCommand(Guid ConversationId, string UserId, string Message) : IRequest<ConversationDto>;

public class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, ConversationDto>
{
    private const int MaxHistoryMessages = 20;
    private const int MaxContextChunks = 6;

    private readonly IChatRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAIProvider _aiProvider;
    private readonly IConfiguration _configuration;
    private readonly IEmbeddingService _embeddingService;
    private readonly IQdrantService _qdrantService;
    private readonly IPromptBuilder _promptBuilder;
    private readonly IIntentDetectionService _intentDetectionService;
    private readonly IChatScopeClassifier _scopeClassifier;
    private readonly IGooglePlacesService _googlePlacesService;
    private readonly ILogger<SendMessageCommandHandler> _logger;
    private readonly IAIRequestLogWriter? _telemetryWriter;

    public SendMessageCommandHandler(
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
        ILogger<SendMessageCommandHandler> logger,
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
        _logger = logger;
        _telemetryWriter = telemetryWriter;
    }

    public async Task<ConversationDto> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        var conversation = await _repository.GetByIdAsync(request.ConversationId, request.UserId, cancellationToken);
        if (conversation == null)
        {
            throw new KeyNotFoundException("المحادثة غير موجودة أو لا تملك صلاحية الوصول إليها.");
        }

        var userMsg = new ConversationMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            Role = "user",
            Content = request.Message,
            CreatedAt = DateTime.UtcNow
        };
        conversation.Messages.Add(userMsg);
        await _repository.AddMessageAsync(userMsg, cancellationToken);

        if (conversation.Title == "محادثة جديدة")
        {
            var cleanText = request.Message.Trim();
            conversation.Title = cleanText.Length > 25 ? cleanText.Substring(0, 22) + "..." : cleanText;
        }

        var history = conversation.Messages
            .Where(m => m.Id != userMsg.Id)
            .OrderBy(m => m.CreatedAt)
            .TakeLast(MaxHistoryMessages)
            .Select(m => new ChatMessageDto
            {
                Id = m.Id,
                Role = m.Role,
                Content = m.Content
            })
            .ToList();

        var intentResult = await ResolveIntentAsync(request.Message, history, cancellationToken);
        var intent = intentResult.Intent;
        var queryToSearch = intentResult.StandaloneQuery;
        
        _logger.LogInformation("Detected Chat Intent: {Intent}, Standalone Query: {Query}", intent, queryToSearch);

        if (intent == ChatIntent.OutOfScope)
        {
            var outOfScopeMessage = _configuration["ChatBot:OutOfScopeMessage"] 
                ?? "عذراً، أنا مساعد Safarni السياحي، ويمكنني مساعدتك فقط في كل ما يتعلق بالسفر والسياحة. يرجى طرح سؤال متعلق بالسياحة.";
            
            var directResponseMessage = new ConversationMessage
            {
                Id = Guid.NewGuid(),
                ConversationId = conversation.Id,
                Role = "assistant",
                Content = outOfScopeMessage,
                CreatedAt = DateTime.UtcNow
            };

            conversation.Messages.Add(directResponseMessage);
            await _repository.AddMessageAsync(directResponseMessage, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ToConversationDto(conversation);
        }

        if (intent == ChatIntent.Social)
        {
            _logger.LogInformation(
                "Sending Social intent directly to the model without RAG or Places lookup.");
            var directSystemPrompt = _promptBuilder.BuildDirectSystemPrompt(intent, request.Message);
            var directUserPrompt = _promptBuilder.BuildUserPrompt(request.Message);
            var generatedResponse = await GetCompletionWithTelemetryAsync(
                request.UserId,
                request.ConversationId,
                directSystemPrompt,
                history,
                directUserPrompt,
                "Chat.Completion",
                cancellationToken);

            var directResponseMessage = new ConversationMessage
            {
                Id = Guid.NewGuid(),
                ConversationId = conversation.Id,
                Role = "assistant",
                Content = generatedResponse,
                CreatedAt = DateTime.UtcNow
            };

            conversation.Messages.Add(directResponseMessage);
            await _repository.AddMessageAsync(directResponseMessage, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ToConversationDto(conversation);
        }

        var clarification = TripPlanningClarifier.BuildClarification(
            intentResult.IsTripPlanningRequest,
            request.Message,
            history);

        if (!string.IsNullOrWhiteSpace(clarification))
        {
            var clarificationMessage = new ConversationMessage
            {
                Id = Guid.NewGuid(),
                ConversationId = conversation.Id,
                Role = "assistant",
                Content = clarification,
                CreatedAt = DateTime.UtcNow
            };

            conversation.Messages.Add(clarificationMessage);
            await _repository.AddMessageAsync(clarificationMessage, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return ToConversationDto(conversation);
        }

        // STEP 1: Search Database FIRST (RAG)
        _logger.LogInformation("Step 1: Searching RAG Database for message context: {Message}", queryToSearch);
        List<DocumentChunk>? contextChunks = null;
        try
        {
            if (await _qdrantService.IsHealthyAsync())
            {
                var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(queryToSearch);
                var topK = int.TryParse(_configuration["RAG:TopK"], out var k) ? k : MaxContextChunks;
                contextChunks = await _qdrantService.SearchSimilarChunksAsync(queryEmbedding, Math.Min(topK, MaxContextChunks));
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Qdrant vector search was unavailable or failed. Using direct SQL Server knowledge fallback.");
        }

        if (contextChunks == null || contextChunks.Count == 0)
        {
            contextChunks = await GetSqlFallbackChunksAsync(queryToSearch, cancellationToken);
        }

        if (contextChunks.Count > MaxContextChunks)
        {
            contextChunks = contextChunks.Take(MaxContextChunks).ToList();
        }

        string systemPrompt;
        bool foundInDb = contextChunks != null && contextChunks.Count > 0;

        if (foundInDb && intent == ChatIntent.RagOnly)
        {
            _logger.LogInformation("Answer found in RAG Database ({Count} chunks). Responding from Database.", contextChunks!.Count);
            systemPrompt = _promptBuilder.BuildSystemPrompt(contextChunks!, request.Message);
        }
        else
        {
            // If intent is RagOnly, do not query Google Places API at all to speed up greetings and conversational messages
            if (intent == ChatIntent.RagOnly)
            {
                _logger.LogInformation("Intent is RagOnly and not found in database. Bypassing Google Places and responding using general travel knowledge.");
                systemPrompt = _promptBuilder.BuildSystemPrompt(new List<DocumentChunk>(), request.Message);
            }
            else
            {
                // STEP 2: If NOT in Database -> Check Google Places API
                _logger.LogInformation("Step 2: No data in Database. Searching Google Places API for message: {Message}", queryToSearch);
                List<PlaceDetails>? places = null;
                try
                {
                    places = await _googlePlacesService.TextSearchAsync(
                        queryToSearch,
                        cancellationToken,
                        _promptBuilder.GetGooglePlacesLanguageCode(request.Message));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to call Google Places API.");
                }

                bool foundInPlaces = places != null && places.Count > 0;

                if (places != null)
                {
                    _logger.LogInformation("Answer found in Google Places API ({Count} places). Responding from Google Places.", places!.Count);
                    systemPrompt = _promptBuilder.BuildSystemPromptWithPlaces(contextChunks, places, intent, request.Message);
                }
                else
                {
                // STEP 3: NEITHER DB NOR GOOGLE PLACES HAS IT -> Refuse to answer from external AI knowledge!
                _logger.LogInformation("No data found in Database or Google Places API. Returning refusal message.");
                var refusalText = "عفواً، لا يمكنني العثور على هذه المعلومات في قاعدة المعرفة الخاصة بي ولا عبر Google Places.";

                var refusalMsg = new ConversationMessage
                {
                    Id = Guid.NewGuid(),
                    ConversationId = conversation.Id,
                    Role = "assistant",
                    Content = refusalText,
                    CreatedAt = DateTime.UtcNow
                };
                conversation.Messages.Add(refusalMsg);
                await _repository.AddMessageAsync(refusalMsg, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return new ConversationDto
                {
                    Id = conversation.Id,
                    Title = conversation.Title,
                    CreatedAt = conversation.CreatedAt,
                    Messages = conversation.Messages
                        .OrderBy(m => m.CreatedAt)
                        .Select(m => new ChatMessageDto
                        {
                            Role = m.Role,
                            Content = m.Content
                        })
                        .ToList()
                };
            }
        }
    }

        var userPrompt = _promptBuilder.BuildUserPrompt(request.Message);
        _logger.LogInformation("Sending prompt to LLM (System Prompt Length: {Len}).", systemPrompt.Length);
        var aiResponse = await GetCompletionWithTelemetryAsync(
            request.UserId,
            request.ConversationId,
            systemPrompt,
            history,
            userPrompt,
            "Chat.Completion",
            cancellationToken);

        var assistantMsg = new ConversationMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            Role = "assistant",
            Content = aiResponse,
            CreatedAt = DateTime.UtcNow
        };
        conversation.Messages.Add(assistantMsg);
        await _repository.AddMessageAsync(assistantMsg, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToConversationDto(conversation);
    }

    private async Task<string> GetCompletionWithTelemetryAsync(
        string userId,
        Guid conversationId,
        string systemPrompt,
        List<ChatMessageDto> history,
        string userMessage,
        string operation,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        Guid? parsedUserId = Guid.TryParse(userId, out var userIdGuid)
            ? userIdGuid
            : null;

        try
        {
            var reply = await _aiProvider.GetChatCompletionAsync(
                systemPrompt,
                history,
                userMessage,
                cancellationToken);

            stopwatch.Stop();

            await AIRequestTelemetry.RecordAsync(
                _telemetryWriter,
                new AIRequestLogEntry(
                    parsedUserId,
                    conversationId,
                    ItineraryJobId: null,
                    _aiProvider.ProviderName,
                    _aiProvider.ModelName,
                    operation,
                    Status: "Success",
                    stopwatch.ElapsedMilliseconds),
                cancellationToken);

            return reply;
        }
        catch (Exception exception)
        {
            stopwatch.Stop();

            await AIRequestTelemetry.RecordAsync(
                _telemetryWriter,
                new AIRequestLogEntry(
                    parsedUserId,
                    conversationId,
                    ItineraryJobId: null,
                    _aiProvider.ProviderName,
                    _aiProvider.ModelName,
                    operation,
                    Status: "Failure",
                    stopwatch.ElapsedMilliseconds,
                    ErrorCode: exception.GetType().Name),
                cancellationToken);

            throw;
        }
    }

    private static ConversationDto ToConversationDto(Conversation conversation)
    {
        return new ConversationDto
        {
            Id = conversation.Id,
            Title = conversation.Title,
            CreatedAt = conversation.CreatedAt,
            IsPinned = conversation.IsPinned,
            Messages = conversation.Messages
                .OrderBy(m => m.CreatedAt)
                .Select(m => new ChatMessageDto
                {
                    Id = m.Id,
                    Role = m.Role,
                    Content = m.Content
                })
                .ToList()
        };
    }

    private async Task<IntentDetectionResult> ResolveIntentAsync(
        string message,
        List<ChatMessageDto> history,
        CancellationToken cancellationToken)
    {
        var result = await _intentDetectionService.DetectIntentAsync(message, history, cancellationToken);
        
        if (result.Intent == ChatIntent.OutOfScope ||
            !result.RequiresSemanticClassification)
        {
            return result;
        }

        var semanticIntent = await _scopeClassifier.ClassifyAsync(
            message,
            history,
            cancellationToken);

        if (semanticIntent is ChatIntent.OutOfScope or ChatIntent.Social)
        {
            result.Intent = semanticIntent;
            return result;
        }

        result.Intent = result.Intent switch
        {
            ChatIntent.GooglePlacesOnly => ChatIntent.GooglePlacesOnly,
            ChatIntent.Hybrid => ChatIntent.Hybrid,
            _ => ChatIntent.RagOnly
        };

        return result;
    }

    private async Task<List<DocumentChunk>> GetSqlFallbackChunksAsync(string userMessage, CancellationToken cancellationToken)
    {
        var chunks = new List<DocumentChunk>();
        var query = userMessage.ToLower();

        var knowledges = await _repository.GetTravelKnowledgesAsync(cancellationToken);
        foreach (var k in knowledges)
        {
            bool isLocationMatch = false;

            // Check if the query mentions the city
            if (!string.IsNullOrEmpty(k.City))
            {
                var cityParts = k.City.ToLower().Split('/');
                foreach (var part in cityParts)
                {
                    var cleanPart = part.Trim();
                    if (cleanPart.Length >= 3 && query.Contains(cleanPart))
                    {
                        isLocationMatch = true;
                        break;
                    }
                }
            }

            // Check if the query mentions the country
            if (!isLocationMatch && !string.IsNullOrEmpty(k.Country))
            {
                var countryParts = k.Country.ToLower().Split('/');
                foreach (var part in countryParts)
                {
                    var cleanPart = part.Trim();
                    if (cleanPart.Length >= 3 && query.Contains(cleanPart))
                    {
                        isLocationMatch = true;
                        break;
                    }
                }
            }

            // Special case: Schengen visa
            bool isVisaMatch = k.Category.Equals("VisaInfo", StringComparison.OrdinalIgnoreCase) && 
                               (query.Contains("شنغن") || query.Contains("schengen") || query.Contains("فيزا") || query.Contains("تأشيرة") || query.Contains("تاشيره"));

            if (isLocationMatch || isVisaMatch)
            {
                chunks.Add(new DocumentChunk
                {
                    Id = k.Id.ToString(),
                    EntityId = k.Id.ToString(),
                    EntityType = k.Category,
                    Content = $"Category: {k.Category}. Title: {k.Title}. Location: {k.City}, {k.Country}. Content: {k.Content}",
                    City = k.City ?? "",
                    Country = k.Country ?? "",
                    Source = "SQL Server Database",
                    SourceUrl = k.SourceUrl
                });
            }
        }

        return chunks;
    }
}
