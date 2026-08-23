using Safarni.Domain.Entities;
using Safarni.Domain.Enums;

namespace Safarni.Application.Features.Community;

public sealed record CommunityAuthorResponse(
    Guid UserId,
    string DisplayName,
    string? ProfilePhotoUrl,
    string? Bio);

public sealed record CommunityMediaResponse(
    Guid Id,
    string MediaType,
    string Url,
    string? ThumbnailUrl,
    string? AltText,
    int SortOrder);

public sealed record CommunityCommentResponse(
    Guid Id,
    Guid? ParentCommentId,
    string Body,
    bool IsAcceptedAnswer,
    DateTime CreatedAt,
    CommunityAuthorResponse Author,
    bool IsOwnedByCurrentUser,
    IReadOnlyCollection<CommunityMediaResponse> Media);

public sealed record CommunityDestinationTrendResponse(
    string Destination,
    int PostsCount,
    int EngagementCount);

public sealed record CommunityPostResponse(
    Guid Id,
    CommunityPostType Type,
    string Title,
    string Body,
    string? Destination,
    Guid? LinkedTripId,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    CommunityAuthorResponse Author,
    int LikesCount,
    int SavesCount,
    int CommentsCount,
    bool IsOwnedByCurrentUser,
    bool IsLikedByCurrentUser,
    bool IsSavedByCurrentUser,
    IReadOnlyCollection<CommunityMediaResponse> Media,
    IReadOnlyCollection<CommunityCommentResponse>? Comments = null)
{
    public static CommunityAuthorResponse MapAuthor(User user) =>
        new(user.Id, user.IsActive ? user.FullName : "Deleted User", user.IsActive ? user.ProfilePhotoUrl : null, user.IsActive ? user.Bio : null);

    public static CommunityPostResponse Map(
        CommunityPost post,
        Guid? currentUserId,
        bool includeComments = false)
    {
        var comments = includeComments
            ? post.Comments
                .Where(comment => !comment.IsDeleted)
                .OrderBy(comment => comment.CreatedAt)
                .Select(comment => new CommunityCommentResponse(
                    comment.Id,
                    comment.ParentCommentId,
                    comment.Body,
                    comment.IsAcceptedAnswer,
                    comment.CreatedAt,
                    MapAuthor(comment.Author),
                    currentUserId.HasValue && comment.AuthorUserId == currentUserId,
                    comment.Media.OrderBy(media => media.SortOrder)
                        .Select(media => new CommunityMediaResponse(media.Id, media.MediaType, media.Url, media.ThumbnailUrl, media.AltText, media.SortOrder))
                        .ToArray()))
                .ToArray()
            : null;

        return new CommunityPostResponse(
            post.Id,
            post.Type,
            post.Title,
            post.Body,
            post.Destination,
            post.LinkedTripId,
            post.CreatedAt,
            post.UpdatedAt,
            MapAuthor(post.Author),
            post.LikesCount,
            post.SavesCount,
            post.CommentsCount,
            currentUserId.HasValue && post.AuthorUserId == currentUserId,
            currentUserId.HasValue && post.Interactions.Any(i => i.UserId == currentUserId && i.Type == CommunityPostInteractionType.Like),
            currentUserId.HasValue && post.Interactions.Any(i => i.UserId == currentUserId && i.Type == CommunityPostInteractionType.Save),
            post.Media.OrderBy(media => media.SortOrder)
                .Select(media => new CommunityMediaResponse(media.Id, media.MediaType, media.Url, media.ThumbnailUrl, media.AltText, media.SortOrder))
                .ToArray(),
            comments);
    }
}
