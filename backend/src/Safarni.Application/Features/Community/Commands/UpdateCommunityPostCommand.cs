using MediatR;
using Safarni.Application.Abstractions.Authentication;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;

namespace Safarni.Application.Features.Community.Commands;

public sealed record UpdateCommunityPostCommand(
    Guid PostId,
    string Title,
    string Body,
    string? Destination,
    IReadOnlyCollection<CommunityMediaInput>? Media)
    : IRequest<Result<CommunityPostResponse>>;

public sealed class UpdateCommunityPostCommandHandler
    : IRequestHandler<UpdateCommunityPostCommand, Result<CommunityPostResponse>>
{
    private readonly ICommunityRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public UpdateCommunityPostCommandHandler(
        ICommunityRepository repository,
        ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task<Result<CommunityPostResponse>> Handle(
        UpdateCommunityPostCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is not { } userId)
        {
            return Result<CommunityPostResponse>.Failure(
                new Error("Community.Unauthorized", "You must be signed in to edit a post.", ErrorType.Unauthorized));
        }

        var post = await _repository.GetPostAsync(request.PostId, cancellationToken);
        if (post == null || post.Status != Domain.Enums.CommunityPostStatus.Published)
        {
            return Result<CommunityPostResponse>.Failure(
                new Error("Community.PostNotFound", "Community post not found.", ErrorType.NotFound));
        }

        if (post.AuthorUserId != userId)
        {
            return Result<CommunityPostResponse>.Failure(
                new Error("Community.EditForbidden", "You can only edit your own posts.", ErrorType.Forbidden));
        }

        var title = request.Title?.Trim() ?? string.Empty;
        var body = request.Body?.Trim() ?? string.Empty;
        var destination = request.Destination?.Trim();

        if (title.Length is < 3 or > 180)
        {
            return Result<CommunityPostResponse>.Failure(
                new Error("Community.InvalidTitle", "Post titles must be between 3 and 180 characters.", ErrorType.Validation));
        }

        if (body.Length is < 10 or > 12000)
        {
            return Result<CommunityPostResponse>.Failure(
                new Error("Community.InvalidBody", "Post content must be between 10 and 12,000 characters.", ErrorType.Validation));
        }

        if (destination?.Length > 160)
        {
            return Result<CommunityPostResponse>.Failure(
                new Error("Community.InvalidDestination", "Destination is too long.", ErrorType.Validation));
        }

        post.Title = title;
        post.Body = body;
        post.Destination = string.IsNullOrWhiteSpace(destination) ? null : destination;
        post.UpdatedAt = DateTime.UtcNow;

        foreach (var media in request.Media ?? [])
        {
            if (string.IsNullOrWhiteSpace(media.Url) ||
                (!string.Equals(media.MediaType, "image", StringComparison.OrdinalIgnoreCase) &&
                 !string.Equals(media.MediaType, "video", StringComparison.OrdinalIgnoreCase)))
            {
                return Result<CommunityPostResponse>.Failure(
                    new Error("Community.InvalidMedia", "Media must have a valid image or video URL.", ErrorType.Validation));
            }

            post.Media.Add(new Domain.Entities.CommunityPostMedia
            {
                Id = Guid.NewGuid(),
                MediaType = media.MediaType.ToLowerInvariant(),
                Url = media.Url.Trim(),
                ThumbnailUrl = media.ThumbnailUrl?.Trim(),
                AltText = media.AltText?.Trim(),
                SortOrder = post.Media.Count + media.SortOrder
            });
        }

        await _repository.SaveChangesAsync(cancellationToken);

        var savedPost = await _repository.GetPostAsync(post.Id, cancellationToken);
        return Result<CommunityPostResponse>.Success(
            CommunityPostResponse.Map(savedPost ?? post, userId, includeComments: true));
    }
}
