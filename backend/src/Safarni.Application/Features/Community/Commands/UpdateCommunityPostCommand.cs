using MediatR;
using Safarni.Application.Abstractions.Authentication;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Abstractions.Storage;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;
using Safarni.Domain.Entities;
using Safarni.Domain.Enums;

namespace Safarni.Application.Features.Community.Commands;

public sealed record UpdateCommunityPostCommand(
    Guid PostId,
    CommunityPostType Type,
    string Title,
    string Body,
    string? Destination,
    Guid? LinkedTripId,
    IReadOnlyCollection<CommunityMediaInput>? Media)
    : IRequest<Result<CommunityPostResponse>>;

public sealed class UpdateCommunityPostCommandHandler
    : IRequestHandler<UpdateCommunityPostCommand, Result<CommunityPostResponse>>
{
    private readonly ICommunityRepository _repository;
    private readonly ICurrentUserService _currentUser;
    private readonly ICommunityMediaStorage _storage;

    public UpdateCommunityPostCommandHandler(
        ICommunityRepository repository,
        ICurrentUserService currentUser,
        ICommunityMediaStorage storage)
    {
        _repository = repository;
        _currentUser = currentUser;
        _storage = storage;
    }

    public async Task<Result<CommunityPostResponse>> Handle(UpdateCommunityPostCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
            return Result<CommunityPostResponse>.Failure(new Error("Community.Unauthorized", "You must be signed in to edit a post.", ErrorType.Unauthorized));

        var post = await _repository.GetPostForUpdateAsync(request.PostId, cancellationToken);
        if (post is null || post.Status != CommunityPostStatus.Published)
            return Result<CommunityPostResponse>.Failure(new Error("Community.PostNotFound", "Community post not found.", ErrorType.NotFound));
        if (post.AuthorUserId != userId)
            return Result<CommunityPostResponse>.Failure(new Error("Community.Forbidden", "You can only edit your own posts.", ErrorType.Forbidden));

        var title = request.Title?.Trim() ?? string.Empty;
        var body = request.Body?.Trim() ?? string.Empty;
        var destination = request.Destination?.Trim();
        var mediaInputs = request.Media ?? [];

        if (!Enum.IsDefined(request.Type))
            return Invalid("Community.InvalidType", "Choose a valid post type.");
        if (title.Length is < 3 or > 180)
            return Invalid("Community.InvalidTitle", "Post titles must be between 3 and 180 characters.");
        if (body.Length is < 10 or > 12000)
            return Invalid("Community.InvalidBody", "Post content must be between 10 and 12,000 characters.");
        if (destination?.Length > 160)
            return Invalid("Community.InvalidDestination", "Destination is too long.");
        if (mediaInputs.Count > 5)
            return Invalid("Community.TooManyMedia", "A post can contain up to five media files.");
        if (request.LinkedTripId is { } tripId && !await _repository.IsTripOwnedByUserAsync(tripId, userId, cancellationToken))
            return Invalid("Community.InvalidTrip", "You can only attach one of your own trips.");
        if (mediaInputs.Any(media => !IsValidMedia(media)))
            return Invalid("Community.InvalidMedia", "Media must have a valid image or video URL.");

        var oldUrls = post.Media.Select(media => media.Url).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var retainedUrls = mediaInputs.Select(media => media.Url.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var oldMedia = post.Media.ToArray();
        _repository.RemovePostMedia(oldMedia);
        post.Media.Clear();

        post.Type = request.Type;
        post.Title = title;
        post.Body = body;
        post.Destination = string.IsNullOrWhiteSpace(destination) ? null : destination;
        post.LinkedTripId = request.LinkedTripId;
        post.UpdatedAt = DateTime.UtcNow;
        var replacementMedia = mediaInputs.Select(media => new CommunityPostMedia
        {
            Id = Guid.NewGuid(),
            PostId = post.Id,
            MediaType = media.MediaType.ToLowerInvariant(),
            Url = media.Url.Trim(),
            ThumbnailUrl = media.ThumbnailUrl?.Trim(),
            AltText = media.AltText?.Trim(),
            SortOrder = media.SortOrder
        }).ToArray();

        await _repository.AddPostMediaAsync(replacementMedia, cancellationToken);

        await _repository.SaveChangesAsync(cancellationToken);
        foreach (var removedUrl in oldUrls.Except(retainedUrls, StringComparer.OrdinalIgnoreCase))
            await _storage.DeleteAsync(removedUrl, cancellationToken);

        var updated = await _repository.GetPostAsync(post.Id, cancellationToken);
        return Result<CommunityPostResponse>.Success(CommunityPostResponse.Map(updated ?? post, userId));
    }

    private static bool IsValidMedia(CommunityMediaInput media) =>
        !string.IsNullOrWhiteSpace(media.Url) &&
        (string.Equals(media.MediaType, "image", StringComparison.OrdinalIgnoreCase) ||
         string.Equals(media.MediaType, "video", StringComparison.OrdinalIgnoreCase));

    private static Result<CommunityPostResponse> Invalid(string code, string message) =>
        Result<CommunityPostResponse>.Failure(new Error(code, message, ErrorType.Validation));
}
