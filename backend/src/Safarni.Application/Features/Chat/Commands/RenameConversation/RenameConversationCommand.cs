using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ChatBotApi.Application.Common.DTOs;
using ChatBotApi.Application.Common.Interfaces;
using MediatR;

namespace ChatBotApi.Application.Features.Chat.Commands.RenameConversation;

public record RenameConversationCommand(Guid Id, string UserId, string NewTitle) : IRequest<ConversationDto>;

public class RenameConversationCommandHandler : IRequestHandler<RenameConversationCommand, ConversationDto>
{
    private readonly IChatRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public RenameConversationCommandHandler(IChatRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ConversationDto> Handle(RenameConversationCommand request, CancellationToken cancellationToken)
    {
        var conversation = await _repository.GetByIdAsync(request.Id, request.UserId, cancellationToken);
        if (conversation == null)
        {
            throw new KeyNotFoundException("المحادثة غير موجودة أو لا تملك صلاحية الوصول إليها.");
        }

        conversation.Title = string.IsNullOrWhiteSpace(request.NewTitle) ? "محادثة جديدة" : request.NewTitle.Trim();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ConversationDto
        {
            Id = conversation.Id,
            Title = conversation.Title,
            CreatedAt = conversation.CreatedAt,
            IsPinned = conversation.IsPinned,
            Messages = new() // Usually empty for simple updates unless requested
        };
    }
}
