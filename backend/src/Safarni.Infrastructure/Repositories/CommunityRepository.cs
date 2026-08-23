using Microsoft.EntityFrameworkCore;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Domain.Entities;
using Safarni.Domain.Enums;
using Safarni.Infrastructure.Persistence;

namespace Safarni.Infrastructure.Repositories;

internal sealed class CommunityRepository : ICommunityRepository
{
    private readonly ApplicationDbContext _dbContext;

    public CommunityRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<CommunityPost>> ListPublishedPostsAsync(
        CommunityPostType? type,
        string? destination,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.CommunityPosts
            .AsNoTracking()
            .Include(post => post.Author)
            .Include(post => post.Media)
            .Include(post => post.Interactions)
            .Where(post => post.Status == CommunityPostStatus.Published);

        if (type.HasValue)
        {
            query = query.Where(post => post.Type == type.Value);
        }

        if (!string.IsNullOrWhiteSpace(destination))
        {
            query = query.Where(post => post.Destination != null && post.Destination.Contains(destination));
        }

        return await query
            .OrderByDescending(post => post.CreatedAt)
            .Take(Math.Clamp(take, 1, 50))
            .ToListAsync(cancellationToken);
    }

    public Task<CommunityPost?> GetPostAsync(
        Guid postId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.CommunityPosts
            .Include(post => post.Author)
            .Include(post => post.Media)
            .Include(post => post.Interactions)
            .Include(post => post.Comments.Where(comment => !comment.IsDeleted))
                .ThenInclude(comment => comment.Author)
            .Include(post => post.Comments.Where(comment => !comment.IsDeleted))
                .ThenInclude(comment => comment.Media)
            .Include(post => post.Comments.Where(comment => !comment.IsDeleted))
                .ThenInclude(comment => comment.Replies.Where(reply => !reply.IsDeleted))
                    .ThenInclude(reply => reply.Author)
            .Include(post => post.Comments.Where(comment => !comment.IsDeleted))
                .ThenInclude(comment => comment.Replies.Where(reply => !reply.IsDeleted))
                    .ThenInclude(reply => reply.Media)
            .FirstOrDefaultAsync(post => post.Id == postId, cancellationToken);
    }

    public Task<CommunityPost?> GetPostForUpdateAsync(
        Guid postId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.CommunityPosts
            .Include(post => post.Author)
            .Include(post => post.Media)
            .Include(post => post.Interactions)
            .FirstOrDefaultAsync(post => post.Id == postId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<CommunityPost>> ListSavedPostsAsync(
        Guid userId,
        int take,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.CommunityPosts
            .AsNoTracking()
            .Include(post => post.Author)
            .Include(post => post.Media)
            .Include(post => post.Interactions)
            .Where(post => post.Status == CommunityPostStatus.Published &&
                post.Interactions.Any(interaction => interaction.UserId == userId && interaction.Type == CommunityPostInteractionType.Save))
            .OrderByDescending(post => post.Interactions
                .Where(interaction => interaction.UserId == userId && interaction.Type == CommunityPostInteractionType.Save)
                .Max(interaction => interaction.CreatedAt))
            .Take(Math.Clamp(take, 1, 30))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<CommunityDestinationTrend>> ListTrendingDestinationsAsync(
        int take,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.CommunityPosts
            .AsNoTracking()
            .Where(post => post.Status == CommunityPostStatus.Published && post.Destination != null && post.Destination != "")
            .GroupBy(post => post.Destination!)
            .Select(group => new CommunityDestinationTrend(
                group.Key,
                group.Count(),
                group.Sum(post => post.LikesCount + post.SavesCount + post.CommentsCount)))
            .OrderByDescending(destination => destination.EngagementCount)
            .ThenByDescending(destination => destination.PostsCount)
            .Take(Math.Clamp(take, 1, 20))
            .ToListAsync(cancellationToken);
    }

    public Task<CommunityPost?> GetPostWithInteractionsAsync(
        Guid postId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.CommunityPosts
            .Include(post => post.Interactions)
            .FirstOrDefaultAsync(post => post.Id == postId, cancellationToken);
    }

    public Task<CommunityPostComment?> GetCommentAsync(
        Guid commentId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.CommunityPostComments
            .Include(comment => comment.Post)
            .FirstOrDefaultAsync(comment => comment.Id == commentId, cancellationToken);
    }

    public Task<bool> IsTripOwnedByUserAsync(
        Guid tripId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Trips.AnyAsync(trip => trip.Id == tripId && trip.UserId == userId, cancellationToken);
    }

    public async Task AddPostAsync(CommunityPost post, CancellationToken cancellationToken = default)
    {
        await _dbContext.CommunityPosts.AddAsync(post, cancellationToken);
    }

    public Task AddPostMediaAsync(
        IEnumerable<CommunityPostMedia> media,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.CommunityPostMedia.AddRangeAsync(media, cancellationToken);
    }

    public async Task AddCommentAsync(CommunityPostComment comment, CancellationToken cancellationToken = default)
    {
        await _dbContext.CommunityPostComments.AddAsync(comment, cancellationToken);
    }

    public async Task AddInteractionAsync(CommunityPostInteraction interaction, CancellationToken cancellationToken = default)
    {
        await _dbContext.CommunityPostInteractions.AddAsync(interaction, cancellationToken);
    }

    public void RemoveInteraction(CommunityPostInteraction interaction)
    {
        _dbContext.CommunityPostInteractions.Remove(interaction);
    }

    public void RemovePostMedia(IEnumerable<CommunityPostMedia> media)
    {
        _dbContext.CommunityPostMedia.RemoveRange(media);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
