using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ChatBotApi.Application.Common.DTOs;
using ChatBotApi.Application.Common.Interfaces;
using MediatR;

namespace ChatBotApi.Application.Features.Chat.Queries.GetFolders;

public record GetFoldersQuery(string UserId) : IRequest<List<ChatFolderWithConversationsDto>>;

public class GetFoldersQueryHandler : IRequestHandler<GetFoldersQuery, List<ChatFolderWithConversationsDto>>
{
    private readonly IChatRepository _repository;

    public GetFoldersQueryHandler(IChatRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<ChatFolderWithConversationsDto>> Handle(GetFoldersQuery request, CancellationToken cancellationToken)
    {
        var folders = await _repository.GetFoldersByUserIdAsync(request.UserId, cancellationToken);
        
        return folders.Select(f => new ChatFolderWithConversationsDto
        {
            Id = f.Id,
            Name = f.Name,
            CreatedAt = f.CreatedAt,
            Conversations = f.Conversations.Select(c => new ConversationSummaryDto
            {
                Id = c.Id,
                Title = c.Title,
                CreatedAt = c.CreatedAt,
                IsPinned = c.IsPinned
            }).OrderByDescending(c => c.CreatedAt).ToList()
        }).OrderByDescending(f => f.CreatedAt).ToList();
    }
}
