using MediatR;
using Safarni.Application.Abstractions.Authentication;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Abstractions.Storage;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;
using Safarni.Domain.Enums;

namespace Safarni.Application.Features.Community.Commands;

public sealed record DeleteCommunityPostCommand(Guid PostId) : IRequest<Result<Guid>>;

public sealed class DeleteCommunityPostCommandHandler : IRequestHandler<DeleteCommunityPostCommand, Result<Guid>>
{
    private readonly ICommunityRepository _repository;
    private readonly ICurrentUserService _currentUser;
    private readonly ICommunityMediaStorage _storage;

    public DeleteCommunityPostCommandHandler(ICommunityRepository repository, ICurrentUserService currentUser, ICommunityMediaStorage storage)
    {
        _repository = repository;
        _currentUser = currentUser;
        _storage = storage;
    }

    public async Task<Result<Guid>> Handle(DeleteCommunityPostCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
            return Result<Guid>.Failure(new Error("Community.Unauthorized", "You must be signed in to delete a post.", ErrorType.Unauthorized));

        var post = await _repository.GetPostForUpdateAsync(request.PostId, cancellationToken);
        if (post is null || post.Status != CommunityPostStatus.Published)
            return Result<Guid>.Failure(new Error("Community.PostNotFound", "Community post not found.", ErrorType.NotFound));
        if (post.AuthorUserId != userId)
            return Result<Guid>.Failure(new Error("Community.Forbidden", "You can only delete your own posts.", ErrorType.Forbidden));

        var mediaUrls = post.Media.Select(media => media.Url).ToArray();
        post.Status = CommunityPostStatus.Deleted;
        post.UpdatedAt = DateTime.UtcNow;
        await _repository.SaveChangesAsync(cancellationToken);

        foreach (var mediaUrl in mediaUrls)
            await _storage.DeleteAsync(mediaUrl, cancellationToken);

        return Result<Guid>.Success(post.Id);
    }
}
