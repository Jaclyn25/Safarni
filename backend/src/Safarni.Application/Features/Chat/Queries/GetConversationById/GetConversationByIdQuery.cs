using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ChatBotApi.Application.Common.DTOs;
using ChatBotApi.Application.Common.Interfaces;
using MediatR;

namespace ChatBotApi.Application.Features.Chat.Queries.GetConversationById;

public record GetConversationByIdQuery(Guid Id, string UserId) : IRequest<ConversationDto>;

public class GetConversationByIdQueryHandler : IRequestHandler<GetConversationByIdQuery, ConversationDto>
{
    private readonly IChatRepository _repository;

    public GetConversationByIdQueryHandler(IChatRepository repository)
    {
        _repository = repository;
    }

    public async Task<ConversationDto> Handle(GetConversationByIdQuery request, CancellationToken cancellationToken)
    {
        var conversation = await _repository.GetByIdAsync(request.Id, request.UserId, cancellationToken);
        if (conversation == null)
        {
            throw new KeyNotFoundException("المحادثة غير موجودة أو لا تملك صلاحية الوصول إليها.");
        }

        return new ConversationDto
        {
            Id = conversation.Id,
            Title = conversation.Title,
            CreatedAt = conversation.CreatedAt,
            IsPinned = conversation.IsPinned,
            FolderId = conversation.FolderId,
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
