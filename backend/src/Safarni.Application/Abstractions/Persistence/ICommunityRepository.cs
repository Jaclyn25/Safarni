using Safarni.Domain.Entities;
using Safarni.Domain.Enums;

namespace Safarni.Application.Abstractions.Persistence;

public interface ICommunityRepository
{
    Task<IReadOnlyCollection<CommunityPost>> ListPublishedPostsAsync(
        CommunityPostType? type,
        string? destination,
        int take,
        CancellationToken cancellationToken = default);

    Task<CommunityPost?> GetPostAsync(
        Guid postId,
        CancellationToken cancellationToken = default);

    Task<CommunityPost?> GetPostWithInteractionsAsync(
        Guid postId,
        CancellationToken cancellationToken = default);

    Task<CommunityPostComment?> GetCommentAsync(
        Guid commentId,
        CancellationToken cancellationToken = default);

    Task<bool> IsTripOwnedByUserAsync(
        Guid tripId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task AddPostAsync(CommunityPost post, CancellationToken cancellationToken = default);
    Task AddCommentAsync(CommunityPostComment comment, CancellationToken cancellationToken = default);
    Task AddInteractionAsync(CommunityPostInteraction interaction, CancellationToken cancellationToken = default);
    void RemoveInteraction(CommunityPostInteraction interaction);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
