using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Safarni.Application.Abstractions.Notifications;
using Safarni.Domain.Entities;
using Safarni.Domain.Enums;
using Safarni.Infrastructure.Persistence;

namespace Safarni.Infrastructure.Notifications;

public sealed class MarketplaceNotificationService
    : IMarketplaceNotificationService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<MarketplaceNotificationService> _logger;

    public MarketplaceNotificationService(
        ApplicationDbContext dbContext,
        ILogger<MarketplaceNotificationService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public Task NotifyUserAsync(
        Guid recipientUserId,
        MarketplaceNotificationType type,
        string title,
        string message,
        string? actionUrl,
        CancellationToken cancellationToken = default)
    {
        return PersistSafelyAsync(
            [recipientUserId],
            type,
            title,
            message,
            actionUrl,
            cancellationToken);
    }

    public async Task NotifyAgencyReviewersAsync(
        MarketplaceNotificationType type,
        string title,
        string message,
        string? actionUrl,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var recipientIds = await _dbContext.Users
                .AsNoTracking()
                .Where(user => user.IsActive &&
                    (user.Role == UserRole.Admin || user.Role == UserRole.Support))
                .Select(user => user.Id)
                .ToListAsync(cancellationToken);

            await PersistSafelyAsync(
                recipientIds,
                type,
                title,
                message,
                actionUrl,
                cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Could not create marketplace notifications for agency reviewers.");
        }
    }

    private async Task PersistSafelyAsync(
        IEnumerable<Guid> recipientIds,
        MarketplaceNotificationType type,
        string title,
        string message,
        string? actionUrl,
        CancellationToken cancellationToken)
    {
        try
        {
            var now = DateTime.UtcNow;
            var notifications = recipientIds.Distinct().Select(recipientId =>
                new UserNotification
                {
                    Id = Guid.NewGuid(),
                    RecipientUserId = recipientId,
                    Type = type,
                    Title = title,
                    Message = message,
                    ActionUrl = actionUrl,
                    IsRead = false,
                    CreatedAt = now
                });

            await _dbContext.UserNotifications.AddRangeAsync(
                notifications,
                cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Could not persist marketplace notification {NotificationType}.",
                type);
        }
    }
}
