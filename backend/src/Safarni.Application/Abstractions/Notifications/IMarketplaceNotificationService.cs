using Safarni.Domain.Enums;

namespace Safarni.Application.Abstractions.Notifications;

/// <summary>
/// Persists marketplace notifications without allowing a notification failure
/// to roll back the business action that produced it.
/// </summary>
public interface IMarketplaceNotificationService
{
    Task NotifyUserAsync(
        Guid recipientUserId,
        MarketplaceNotificationType type,
        string title,
        string message,
        string? actionUrl,
        CancellationToken cancellationToken = default);

    Task NotifyAgencyReviewersAsync(
        MarketplaceNotificationType type,
        string title,
        string message,
        string? actionUrl,
        CancellationToken cancellationToken = default);
}
