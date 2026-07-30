using System;
using System.Threading;
using System.Threading.Tasks;
using ChatBotApi.Application.Common.DTOs;
using ChatBotApi.Application.Common.Interfaces;
using ChatBotApi.Domain.Entities;
using MediatR;

namespace ChatBotApi.Application.Features.Chat.Commands.CreateConversation;

public record CreateConversationCommand(string UserId, string? Title) : IRequest<ConversationDto>;

public class CreateConversationCommandHandler : IRequestHandler<CreateConversationCommand, ConversationDto>
{
    private readonly IChatRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateConversationCommandHandler(IChatRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ConversationDto> Handle(CreateConversationCommand request, CancellationToken cancellationToken)
    {
        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            Title = string.IsNullOrWhiteSpace(request.Title) ? "محادثة جديدة" : request.Title,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(conversation, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ConversationDto
        {
            Id = conversation.Id,
            Title = conversation.Title,
            CreatedAt = conversation.CreatedAt,
            Messages = new()
        };
    }
}
