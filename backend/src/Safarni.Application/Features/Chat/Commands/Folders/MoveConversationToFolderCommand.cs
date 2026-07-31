using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ChatBotApi.Application.Common.Interfaces;
using MediatR;

namespace ChatBotApi.Application.Features.Chat.Commands.Folders;

public record MoveConversationToFolderCommand(Guid ConversationId, Guid? FolderId, string UserId) : IRequest<bool>;

public class MoveConversationToFolderCommandHandler : IRequestHandler<MoveConversationToFolderCommand, bool>
{
    private readonly IChatRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public MoveConversationToFolderCommandHandler(IChatRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(MoveConversationToFolderCommand request, CancellationToken cancellationToken)
    {
        var conversation = await _repository.GetByIdAsync(request.ConversationId, request.UserId, cancellationToken);
        if (conversation == null)
            throw new KeyNotFoundException("Conversation not found.");

        if (request.FolderId.HasValue)
        {
            var folder = await _repository.GetFolderByIdAsync(request.FolderId.Value, request.UserId, cancellationToken);
            if (folder == null)
                throw new KeyNotFoundException("Folder not found.");
        }

        conversation.FolderId = request.FolderId;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
