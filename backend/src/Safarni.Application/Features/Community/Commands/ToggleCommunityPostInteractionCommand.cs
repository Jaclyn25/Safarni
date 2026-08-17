using MediatR;
using Safarni.Application.Abstractions.Authentication;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;
using Safarni.Domain.Entities;
using Safarni.Domain.Enums;

namespace Safarni.Application.Features.Community.Commands;

public sealed record ToggleCommunityPostInteractionCommand(
    Guid PostId,
    CommunityPostInteractionType Type)
    : IRequest<Result<ToggleCommunityPostInteractionResult>>;

public sealed record ToggleCommunityPostInteractionResult(
    Guid PostId,
    CommunityPostInteractionType Type,
    int NewCount,
    bool IsActive);

public sealed class ToggleCommunityPostInteractionCommandHandler
    : IRequestHandler<ToggleCommunityPostInteractionCommand, Result<ToggleCommunityPostInteractionResult>>
{
    private readonly ICommunityRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public ToggleCommunityPostInteractionCommandHandler(
        ICommunityRepository repository,
        ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task<Result<ToggleCommunityPostInteractionResult>> Handle(
        ToggleCommunityPostInteractionCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is not { } userId)
        {
            return Result<ToggleCommunityPostInteractionResult>.Failure(
                new Error("Community.Unauthorized", "You must be signed in to interact with posts.", ErrorType.Unauthorized));
        }

        if (request.Type is not (CommunityPostInteractionType.Like or CommunityPostInteractionType.Save))
        {
            return Result<ToggleCommunityPostInteractionResult>.Failure(
                new Error("Community.InvalidInteraction", "Unsupported post interaction.", ErrorType.Validation));
        }

        var post = await _repository.GetPostWithInteractionsAsync(request.PostId, cancellationToken);
        if (post == null || post.Status != CommunityPostStatus.Published)
        {
            return Result<ToggleCommunityPostInteractionResult>.Failure(
                new Error("Community.PostNotFound", "Community post not found.", ErrorType.NotFound));
        }

        var existing = post.Interactions.FirstOrDefault(i => i.UserId == userId && i.Type == request.Type);
        var isActive = existing == null;

        if (existing != null)
        {
            _repository.RemoveInteraction(existing);
        }
        else
        {
            await _repository.AddInteractionAsync(new CommunityPostInteraction
            {
                Id = Guid.NewGuid(),
                PostId = post.Id,
                UserId = userId,
                Type = request.Type,
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);
        }

        if (request.Type == CommunityPostInteractionType.Like)
        {
            post.LikesCount = Math.Max(0, post.LikesCount + (isActive ? 1 : -1));
        }
        else
        {
            post.SavesCount = Math.Max(0, post.SavesCount + (isActive ? 1 : -1));
        }

        await _repository.SaveChangesAsync(cancellationToken);

        return Result<ToggleCommunityPostInteractionResult>.Success(
            new ToggleCommunityPostInteractionResult(
                post.Id,
                request.Type,
                request.Type == CommunityPostInteractionType.Like ? post.LikesCount : post.SavesCount,
                isActive));
    }
}
