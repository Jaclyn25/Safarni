using MediatR;
using Safarni.Application.Abstractions.Authentication;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;
using Safarni.Domain.Entities;
using Safarni.Domain.Enums;

namespace Safarni.Application.Features.Community.Commands;

public sealed record CreateCommunityCommentCommand(
    Guid PostId,
    string Body,
    Guid? ParentCommentId = null)
    : IRequest<Result<CreateCommunityCommentResult>>;

public sealed record CreateCommunityCommentResult(Guid CommentId, int NewCommentsCount);

public sealed class CreateCommunityCommentCommandHandler
    : IRequestHandler<CreateCommunityCommentCommand, Result<CreateCommunityCommentResult>>
{
    private readonly ICommunityRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public CreateCommunityCommentCommandHandler(
        ICommunityRepository repository,
        ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task<Result<CreateCommunityCommentResult>> Handle(
        CreateCommunityCommentCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is not { } userId)
        {
            return Result<CreateCommunityCommentResult>.Failure(
                new Error("Community.Unauthorized", "You must be signed in to comment.", ErrorType.Unauthorized));
        }

        var body = request.Body?.Trim() ?? string.Empty;
        if (body.Length is < 1 or > 3000)
        {
            return Result<CreateCommunityCommentResult>.Failure(
                new Error("Community.InvalidComment", "Comments must be between 1 and 3,000 characters.", ErrorType.Validation));
        }

        var post = await _repository.GetPostWithInteractionsAsync(request.PostId, cancellationToken);
        if (post == null || post.Status != CommunityPostStatus.Published)
        {
            return Result<CreateCommunityCommentResult>.Failure(
                new Error("Community.PostNotFound", "Community post not found.", ErrorType.NotFound));
        }

        if (request.ParentCommentId is { } parentId)
        {
            var parent = await _repository.GetCommentAsync(parentId, cancellationToken);
            if (parent == null || parent.PostId != request.PostId || parent.IsDeleted)
            {
                return Result<CreateCommunityCommentResult>.Failure(
                    new Error("Community.InvalidParentComment", "The reply target could not be found.", ErrorType.Validation));
            }
        }

        var comment = new CommunityPostComment
        {
            Id = Guid.NewGuid(),
            PostId = request.PostId,
            AuthorUserId = userId,
            ParentCommentId = request.ParentCommentId,
            Body = body,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddCommentAsync(comment, cancellationToken);
        post.CommentsCount++;
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<CreateCommunityCommentResult>.Success(
            new CreateCommunityCommentResult(comment.Id, post.CommentsCount));
    }
}
