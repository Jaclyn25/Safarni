using System;
using System.Threading;
using System.Threading.Tasks;
using ChatBotApi.Application.Common.DTOs;
using ChatBotApi.Application.Common.Interfaces;
using ChatBotApi.Domain.Entities;
using MediatR;

namespace ChatBotApi.Application.Features.Chat.Commands.CreateFolder;

public record CreateFolderCommand(string UserId, string Name) : IRequest<ChatFolderDto>;

public class CreateFolderCommandHandler : IRequestHandler<CreateFolderCommand, ChatFolderDto>
{
    private readonly IChatRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateFolderCommandHandler(IChatRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ChatFolderDto> Handle(CreateFolderCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Folder name cannot be empty.");

        var folder = new ChatFolder
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            Name = request.Name.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddFolderAsync(folder, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ChatFolderDto
        {
            Id = folder.Id,
            Name = folder.Name,
            CreatedAt = folder.CreatedAt
        };
    }
}
