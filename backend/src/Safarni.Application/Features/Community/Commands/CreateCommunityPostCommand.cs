using MediatR;
using Safarni.Application.Abstractions.Authentication;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;
using Safarni.Domain.Entities;
using Safarni.Domain.Enums;

namespace Safarni.Application.Features.Community.Commands;

public sealed record CommunityMediaInput(
    string MediaType,
    string Url,
    string? ThumbnailUrl = null,
    string? AltText = null,
    int SortOrder = 0);

public sealed record CreateCommunityPostCommand(
    CommunityPostType Type,
    string Title,
    string Body,
    string? Destination,
    Guid? LinkedTripId,
    IReadOnlyCollection<CommunityMediaInput>? Media)
    : IRequest<Result<CommunityPostResponse>>;

public sealed class CreateCommunityPostCommandHandler
    : IRequestHandler<CreateCommunityPostCommand, Result<CommunityPostResponse>>
{
    private readonly ICommunityRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public CreateCommunityPostCommandHandler(
        ICommunityRepository repository,
        ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task<Result<CommunityPostResponse>> Handle(
        CreateCommunityPostCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is not { } userId)
        {
            return Result<CommunityPostResponse>.Failure(
                new Error("Community.Unauthorized", "You must be signed in to create a post.", ErrorType.Unauthorized));
        }

        var title = request.Title?.Trim() ?? string.Empty;
        var body = request.Body?.Trim() ?? string.Empty;
        var destination = request.Destination?.Trim();

        if (!Enum.IsDefined(request.Type))
        {
            return Result<CommunityPostResponse>.Failure(
                new Error("Community.InvalidType", "Choose a valid post type.", ErrorType.Validation));
        }

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

        if ((request.Media?.Count ?? 0) > 5)
        {
            return Result<CommunityPostResponse>.Failure(
                new Error("Community.TooManyMedia", "A post can contain up to five media files.", ErrorType.Validation));
        }

        if (request.LinkedTripId is { } linkedTripId &&
            !await _repository.IsTripOwnedByUserAsync(linkedTripId, userId, cancellationToken))
        {
            return Result<CommunityPostResponse>.Failure(
                new Error("Community.InvalidTrip", "You can only attach one of your own trips.", ErrorType.Validation));
        }

        var post = new CommunityPost
        {
            Id = Guid.NewGuid(),
            AuthorUserId = userId,
            Type = request.Type,
            Status = CommunityPostStatus.Published,
            Title = title,
            Body = body,
            Destination = string.IsNullOrWhiteSpace(destination) ? null : destination,
            LinkedTripId = request.LinkedTripId,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var media in request.Media ?? [])
        {
            if (string.IsNullOrWhiteSpace(media.Url) ||
                !string.Equals(media.MediaType, "image", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(media.MediaType, "video", StringComparison.OrdinalIgnoreCase))
            {
                return Result<CommunityPostResponse>.Failure(
                    new Error("Community.InvalidMedia", "Media must have a valid image or video URL.", ErrorType.Validation));
            }

            post.Media.Add(new CommunityPostMedia
            {
                Id = Guid.NewGuid(),
                MediaType = media.MediaType.ToLowerInvariant(),
                Url = media.Url.Trim(),
                ThumbnailUrl = media.ThumbnailUrl?.Trim(),
                AltText = media.AltText?.Trim(),
                SortOrder = media.SortOrder
            });
        }

        await _repository.AddPostAsync(post, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        var savedPost = await _repository.GetPostAsync(post.Id, cancellationToken);
        return Result<CommunityPostResponse>.Success(
            CommunityPostResponse.Map(savedPost ?? post, userId));
    }
}
