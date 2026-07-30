using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ChatBotApi.Application.Common.DTOs;
using ChatBotApi.Application.Common.Interfaces;
using ChatBotApi.Application.Features.Chat.Commands.SendMessage;
using MediatR;

namespace ChatBotApi.Application.Features.Chat.Commands.EditMessage;

public record EditMessageCommand(Guid ConversationId, string UserId, Guid MessageId, string NewContent) : IRequest<ConversationDto>;

public class EditMessageCommandHandler : IRequestHandler<EditMessageCommand, ConversationDto>
{
    private readonly IChatRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMediator _mediator;

    public EditMessageCommandHandler(IChatRepository repository, IUnitOfWork unitOfWork, IMediator mediator)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mediator = mediator;
    }

    public async Task<ConversationDto> Handle(EditMessageCommand request, CancellationToken cancellationToken)
    {
        var conversation = await _repository.GetByIdAsync(request.ConversationId, request.UserId, cancellationToken);
        if (conversation == null)
        {
            throw new KeyNotFoundException("المحادثة غير موجودة أو لا تملك صلاحية الوصول إليها.");
        }

        var messages = conversation.Messages.OrderBy(m => m.CreatedAt).ToList();
        var targetMsg = messages.FirstOrDefault(m => m.Id == request.MessageId);
        
        if (targetMsg == null)
        {
            throw new KeyNotFoundException("الرسالة غير موجودة.");
        }

        if (targetMsg.Role != "user")
        {
            throw new InvalidOperationException("لا يمكنك تعديل رسالة المساعد الذكي.");
        }

        // Delete the target message and all subsequent messages
        var messagesToRemove = messages.Where(m => m.CreatedAt >= targetMsg.CreatedAt).ToList();
        foreach (var msg in messagesToRemove)
        {
            conversation.Messages.Remove(msg);
        }

        if (!conversation.Messages.Any(m => m.Role == "user"))
        {
            var cleanText = request.NewContent.Trim();
            conversation.Title = cleanText.Length > 25 ? cleanText.Substring(0, 22) + "..." : cleanText;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Now, trigger a normal SendMessage with the new content
        var sendCmd = new SendMessageCommand(request.ConversationId, request.UserId, request.NewContent);
        return await _mediator.Send(sendCmd, cancellationToken);
    }
}
