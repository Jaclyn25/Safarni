using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ChatBotApi.Application.Common.Interfaces;
using MediatR;

namespace ChatBotApi.Application.Features.Chat.Commands.DeleteConversation;

public record DeleteConversationCommand(Guid ConversationId, string UserId) : IRequest<bool>;

public class DeleteConversationCommandHandler : IRequestHandler<DeleteConversationCommand, bool>
{
    private readonly IChatRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteConversationCommandHandler(IChatRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteConversationCommand request, CancellationToken cancellationToken)
    {
        var conversation = await _repository.GetByIdAsync(request.ConversationId, request.UserId, cancellationToken);
        if (conversation == null)
        {
            throw new KeyNotFoundException("المحادثة غير موجودة أو لا تملك صلاحية الوصول إليها.");
        }

        _repository.Delete(conversation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
