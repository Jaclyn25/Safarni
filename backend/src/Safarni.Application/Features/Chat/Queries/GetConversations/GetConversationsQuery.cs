using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ChatBotApi.Application.Common.DTOs;
using ChatBotApi.Application.Common.Interfaces;
using MediatR;

namespace ChatBotApi.Application.Features.Chat.Queries.GetConversations;

public record GetConversationsQuery(string UserId) : IRequest<List<ConversationSummaryDto>>;

public class GetConversationsQueryHandler : IRequestHandler<GetConversationsQuery, List<ConversationSummaryDto>>
{
    private readonly IChatRepository _repository;

    public GetConversationsQueryHandler(IChatRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<ConversationSummaryDto>> Handle(GetConversationsQuery request, CancellationToken cancellationToken)
    {
        var conversations = await _repository.GetByUserIdAsync(request.UserId, cancellationToken);
        
        System.Console.WriteLine($"[DEBUG] Total conversations for user {request.UserId}: {conversations.Count}");
        foreach(var c in conversations) {
            System.Console.WriteLine($"[DEBUG] Conv {c.Id} - Title: {c.Title} - Messages: {c.Messages?.Count ?? 0}");
        }

        var filtered = conversations
            .Where(c => c.Messages != null && c.Messages.Count > 1)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new ConversationSummaryDto
            {
                Id = c.Id,
                Title = c.Title,
                CreatedAt = c.CreatedAt
            })
            .ToList();

        System.Console.WriteLine($"[DEBUG] Returned conversations after filter: {filtered.Count}");
        return filtered;
    }
}
