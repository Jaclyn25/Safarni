using Microsoft.EntityFrameworkCore;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Domain.Entities;
using Safarni.Infrastructure.Persistence;

namespace Safarni.Infrastructure.Repositories;

public sealed class UserNotificationRepository : IUserNotificationRepository
{
    private readonly ApplicationDbContext _dbContext;

    public UserNotificationRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<UserNotification>> ListAsync(
        Guid recipientUserId,
        int take,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserNotifications
            .AsNoTracking()
            .Where(notification => notification.RecipientUserId == recipientUserId)
            .OrderByDescending(notification => notification.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountUnreadAsync(
        Guid recipientUserId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.UserNotifications.CountAsync(
            notification => notification.RecipientUserId == recipientUserId &&
                !notification.IsRead,
            cancellationToken);
    }

    public async Task<bool> MarkReadAsync(
        Guid notificationId,
        Guid recipientUserId,
        CancellationToken cancellationToken = default)
    {
        var notification = await _dbContext.UserNotifications.FirstOrDefaultAsync(
            item => item.Id == notificationId &&
                item.RecipientUserId == recipientUserId,
            cancellationToken);
        if (notification is null)
        {
            return false;
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return true;
    }

    public async Task MarkAllReadAsync(
        Guid recipientUserId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        await _dbContext.UserNotifications
            .Where(notification => notification.RecipientUserId == recipientUserId &&
                !notification.IsRead)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(notification => notification.IsRead, true)
                    .SetProperty(notification => notification.ReadAt, now),
                cancellationToken);
    }
}
