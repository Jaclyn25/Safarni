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

namespace ChatBotApi.Application.Features.Chat.Commands.SendMessage;

public record SendMessageCommand(Guid ConversationId, string UserId, string Message) : IRequest<ConversationDto>;

public class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, ConversationDto>
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
    private readonly IGooglePlacesService _googlePlacesService;
    private readonly ILogger<SendMessageCommandHandler> _logger;

    public SendMessageCommandHandler(
        IChatRepository repository, 
        IUnitOfWork unitOfWork, 
        IAIProvider aiProvider, 
        IConfiguration configuration,
        IEmbeddingService embeddingService,
        IQdrantService qdrantService,
        IPromptBuilder promptBuilder,
        IIntentDetectionService intentDetectionService,
        IGooglePlacesService googlePlacesService,
        ILogger<SendMessageCommandHandler> logger)
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
        _logger = logger;
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

        var intentResult = await _intentDetectionService.DetectIntentAsync(request.Message, history, cancellationToken);
        var intent = intentResult.Intent;
        var standaloneQuery = intentResult.StandaloneQuery;

        _logger.LogInformation("Detected Chat Intent: {Intent}, Standalone Query: {Query}", intent, standaloneQuery);

        if (intent == ChatIntent.OutOfDomain)
        {
            var outOfDomainText = _configuration["Groq:RefusalMessage"] ?? "أنا مساعد ذكي متخصص في السياحة والسفر لمنصة سافرني. للأسف لا يمكنني الإجابة على أسئلة خارج هذا التخصص.";
            var outOfDomainMsg = new ConversationMessage
            {
                Id = Guid.NewGuid(),
                ConversationId = conversation.Id,
                Role = "assistant",
                Content = outOfDomainText,
                CreatedAt = DateTime.UtcNow
            };
            conversation.Messages.Add(outOfDomainMsg);
            await _repository.AddMessageAsync(outOfDomainMsg, cancellationToken);
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
                        Id = m.Id,
                        Role = m.Role,
                        Content = m.Content
                    })
                    .ToList()
            };
        }

        // STEP 1: Search Database FIRST (RAG)
        _logger.LogInformation("Step 1: Searching RAG Database for message context: {Message}", standaloneQuery);
        List<DocumentChunk>? contextChunks = null;
        try
        {
            if (await _qdrantService.IsHealthyAsync())
            {
                var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(standaloneQuery);
                var topK = int.TryParse(_configuration["RAG:TopK"], out var k) ? k : MaxContextChunks;
                contextChunks = await _qdrantService.SearchSimilarChunksAsync(queryEmbedding, Math.Min(topK, MaxContextChunks));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Qdrant vector search was unavailable or failed. Using direct SQL Server knowledge fallback.");
        }

        if (contextChunks == null || contextChunks.Count == 0)
        {
            contextChunks = await GetSqlFallbackChunksAsync(standaloneQuery, cancellationToken);
        }

        if (contextChunks.Count > MaxContextChunks)
        {
            contextChunks = contextChunks.Take(MaxContextChunks).ToList();
        }

        string systemPrompt;
        bool foundInDb = contextChunks != null && contextChunks.Count > 0;

        if (foundInDb)
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
                _logger.LogInformation("Step 2: No data in Database. Searching Google Places API for message: {Message}", standaloneQuery);
                List<PlaceDetails>? places = null;
                try
                {
                    places = await _googlePlacesService.TextSearchAsync(
                        standaloneQuery,
                        cancellationToken,
                        _promptBuilder.GetGooglePlacesLanguageCode(standaloneQuery));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to call Google Places API.");
                }

                bool foundInPlaces = places != null && places.Count > 0;

                if (foundInPlaces)
                {
                    _logger.LogInformation("Answer found in Google Places API ({Count} places). Responding from Google Places.", places!.Count);
                    systemPrompt = _promptBuilder.BuildSystemPromptWithPlaces(null, places, intent, request.Message);
                }
                else
                {
                    // STEP 3: NEITHER DB NOR GOOGLE PLACES HAS IT -> Refuse to answer from external AI knowledge!
                    _logger.LogInformation("No data found in Database or Google Places API. Returning refusal message.");
                    var refusalText = _promptBuilder.BuildKnowledgeNotFoundMessage(request.Message);

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
                                Id = m.Id,
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
        var aiResponse = await _aiProvider.GetChatCompletionAsync(systemPrompt, history, userPrompt, cancellationToken);

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
