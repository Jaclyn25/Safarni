using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ChatBotApi.Application.Common.DTOs;
using ChatBotApi.Application.Common.Interfaces;
using MediatR;

namespace ChatBotApi.Application.Features.Chat.Commands.Folders;

public record RenameFolderCommand(Guid Id, Guid UserId, string NewName) : IRequest<ChatFolderDto>;

public class RenameFolderCommandHandler : IRequestHandler<RenameFolderCommand, ChatFolderDto>
{
    private readonly IChatRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public RenameFolderCommandHandler(IChatRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ChatFolderDto> Handle(RenameFolderCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.NewName))
            throw new ArgumentException("Folder name cannot be empty.");

        var folder = await _repository.GetFolderByIdAsync(request.Id, request.UserId, cancellationToken);
        if (folder == null)
            throw new KeyNotFoundException("Folder not found.");

        folder.Name = request.NewName.Trim();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ChatFolderDto
        {
            Id = folder.Id,
            Name = folder.Name,
            CreatedAt = folder.CreatedAt
        };
    }
}
