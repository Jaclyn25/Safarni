using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ChatBotApi.Application.Common.Interfaces;
using MediatR;

namespace ChatBotApi.Application.Features.Chat.Commands.Folders;

public record DeleteFolderCommand(Guid Id, Guid UserId) : IRequest<bool>;

public class DeleteFolderCommandHandler : IRequestHandler<DeleteFolderCommand, bool>
{
    private readonly IChatRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteFolderCommandHandler(IChatRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteFolderCommand request, CancellationToken cancellationToken)
    {
        var folder = await _repository.GetFolderByIdAsync(request.Id, request.UserId, cancellationToken);
        if (folder == null)
            throw new KeyNotFoundException("Folder not found.");

        _repository.DeleteFolder(folder);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
