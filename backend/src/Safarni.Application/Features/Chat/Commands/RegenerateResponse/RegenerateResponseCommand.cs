using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ChatBotApi.Application.Common.DTOs;
using ChatBotApi.Application.Common.Interfaces;
using ChatBotApi.Application.Common.Models.GooglePlaces;
using ChatBotApi.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ChatBotApi.Application.Features.Chat.Commands.RegenerateResponse;

public record RegenerateResponseCommand(Guid ConversationId, string UserId) : IRequest<ConversationDto>;

public class RegenerateResponseCommandHandler : IRequestHandler<RegenerateResponseCommand, ConversationDto>
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
    private readonly IIntentDetectionService _intentDetectionService;
    private readonly IChatScopeClassifier _scopeClassifier;
    private readonly IGooglePlacesService _googlePlacesService;
    private readonly ILogger<RegenerateResponseCommandHandler> _logger;

    public RegenerateResponseCommandHandler(
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
        ILogger<RegenerateResponseCommandHandler> logger)
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
    }

    public async Task<ConversationDto> Handle(RegenerateResponseCommand request, CancellationToken cancellationToken)
    {
        var conversation = await _repository.GetByIdAsync(request.ConversationId, request.UserId, cancellationToken);
        if (conversation == null)
        {
            throw new KeyNotFoundException("المحادثة غير موجودة أو لا تملك صلاحية الوصول إليها.");
        }

        var messages = conversation.Messages.OrderBy(m => m.CreatedAt).ToList();
        var lastUserMsg = messages.LastOrDefault(m => m.Role == "user");
        if (lastUserMsg == null)
        {
            throw new InvalidOperationException("لا توجد رسائل مستخدم لإعادة توليد الرد عليها.");
        }

        var messagesToRemove = messages.Where(m => m.CreatedAt > lastUserMsg.CreatedAt).ToList();
        _repository.DeleteMessages(messagesToRemove);
        foreach (var msg in messagesToRemove)
        {
            conversation.Messages.Remove(msg);
        }

        var history = conversation.Messages
            .Where(m => m.Id != lastUserMsg.Id)
            .OrderBy(m => m.CreatedAt)
            .TakeLast(MaxHistoryMessages)
            .Select(m => new ChatMessageDto
            {
                Id = m.Id,
                Role = m.Role,
                Content = m.Content
            })
            .ToList();

        var intent = await ResolveIntentAsync(lastUserMsg.Content, history, cancellationToken);
        _logger.LogInformation("Regeneration - Detected Chat Intent: {Intent}", intent);

        if (intent is ChatIntent.Social or ChatIntent.OutOfScope)
        {
            _logger.LogInformation(
                "Regeneration is sending {Intent} intent directly to the model without RAG or Places lookup.",
                intent);
            var directSystemPrompt = _promptBuilder.BuildDirectSystemPrompt(intent, lastUserMsg.Content);
            var directUserPrompt = _promptBuilder.BuildUserPrompt(lastUserMsg.Content);
            var generatedResponse = await _aiProvider.GetChatCompletionAsync(
                directSystemPrompt,
                history,
                directUserPrompt,
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

        // STEP 1: Search Database FIRST (RAG)
        _logger.LogInformation("Step 1: Searching RAG Database for message regeneration: {Message}", lastUserMsg.Content);
        List<DocumentChunk>? contextChunks = null;
        try
        {
            if (await _qdrantService.IsHealthyAsync())
            {
                var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(lastUserMsg.Content);
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
            _logger.LogWarning(ex, "Qdrant vector search failed during regeneration.");
        }

        if (contextChunks == null || contextChunks.Count == 0)
        {
            contextChunks = await GetSqlFallbackChunksAsync(lastUserMsg.Content, cancellationToken);
        }

        if (contextChunks.Count > MaxContextChunks)
        {
            contextChunks = contextChunks.Take(MaxContextChunks).ToList();
        }

        string systemPrompt;
        bool foundInDb = contextChunks != null && contextChunks.Count > 0;

        if (foundInDb)
        {
            systemPrompt = _promptBuilder.BuildSystemPrompt(contextChunks!, lastUserMsg.Content);
        }
        else
        {
            // If intent is RagOnly, do not query Google Places API at all to speed up greetings and conversational messages
            if (intent == ChatIntent.RagOnly)
            {
                _logger.LogInformation("Regeneration - Intent is RagOnly and not found in database. Bypassing Google Places and responding using general travel knowledge.");
                systemPrompt = _promptBuilder.BuildSystemPrompt(new List<DocumentChunk>(), lastUserMsg.Content);
            }
            else
            {
                // STEP 2: If NOT in Database -> Check Google Places API
                _logger.LogInformation("Step 2: Searching Google Places API for message regeneration: {Message}", lastUserMsg.Content);
                List<PlaceDetails>? places = null;
                try
                {
                    places = await _googlePlacesService.TextSearchAsync(
                        lastUserMsg.Content,
                        cancellationToken,
                        _promptBuilder.GetGooglePlacesLanguageCode(lastUserMsg.Content));
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to call Google Places API during regeneration.");
                }

                bool foundInPlaces = places != null && places.Count > 0;

                if (foundInPlaces)
                {
                    systemPrompt = _promptBuilder.BuildSystemPromptWithPlaces(null, places, intent, lastUserMsg.Content);
                }
                else
                {
                    // Travel intent, but no live/database data. Fall back to the LLM's general travel knowledge.
                    systemPrompt = _promptBuilder.BuildSystemPrompt(new List<DocumentChunk>(), lastUserMsg.Content);
                }
            }
        }

        var userPrompt = _promptBuilder.BuildUserPrompt(lastUserMsg.Content);
        var newResponse = await _aiProvider.GetChatCompletionAsync(systemPrompt, history, userPrompt, cancellationToken);

        var newAssistantMsg = new ConversationMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            Role = "assistant",
            Content = newResponse,
            CreatedAt = DateTime.UtcNow
        };
        conversation.Messages.Add(newAssistantMsg);
        await _repository.AddMessageAsync(newAssistantMsg, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToConversationDto(conversation);
    }

    private static ConversationDto ToConversationDto(Conversation conversation)
    {
        return new ConversationDto
        {
            Id = conversation.Id,
            Title = conversation.Title,
            CreatedAt = conversation.CreatedAt,
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

    private async Task<ChatIntent> ResolveIntentAsync(
        string message,
        List<ChatMessageDto> history,
        CancellationToken cancellationToken)
    {
        var keywordIntent = _intentDetectionService.DetectIntent(message);
        if (keywordIntent == ChatIntent.OutOfScope)
        {
            return ChatIntent.OutOfScope;
        }

        var semanticIntent = await _scopeClassifier.ClassifyAsync(
            message,
            history,
            cancellationToken);

        if (semanticIntent is ChatIntent.OutOfScope or ChatIntent.Social)
        {
            return semanticIntent;
        }

        return keywordIntent switch
        {
            ChatIntent.GooglePlacesOnly => ChatIntent.GooglePlacesOnly,
            ChatIntent.Hybrid => ChatIntent.Hybrid,
            _ => ChatIntent.RagOnly
        };
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
                    Source = "SQL Server Database"
                });
            }
        }

        return chunks;
    }
}
