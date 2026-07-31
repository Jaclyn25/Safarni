using System;
using System.Threading;
using System.Threading.Tasks;
using ChatBotApi.Application.Common.DTOs;
using ChatBotApi.Application.Common.Interfaces;
using MediatR;

namespace ChatBotApi.Application.Features.Chat.Commands.TogglePin;

public record TogglePinCommand(Guid ConversationId, string UserId) : IRequest<ConversationSummaryDto>;

public class TogglePinCommandHandler : IRequestHandler<TogglePinCommand, ConversationSummaryDto>
{
    private readonly IChatRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public TogglePinCommandHandler(IChatRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ConversationSummaryDto> Handle(TogglePinCommand request, CancellationToken cancellationToken)
    {
        var conversation = await _repository.GetByIdAsync(request.ConversationId, request.UserId, cancellationToken);
        if (conversation == null)
        {
            throw new Exception("Conversation not found or unauthorized");
        }

        conversation.IsPinned = !conversation.IsPinned;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ConversationSummaryDto
        {
            Id = conversation.Id,
            Title = conversation.Title,
            CreatedAt = conversation.CreatedAt,
            IsPinned = conversation.IsPinned
        };
    }
}
