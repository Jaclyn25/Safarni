using Safarni.Domain.Entities;

namespace Safarni.Application.Abstractions.Persistence;

public interface IUserNotificationRepository
{
    Task<IReadOnlyList<UserNotification>> ListAsync(
        Guid recipientUserId,
        int take,
        CancellationToken cancellationToken = default);

    Task<int> CountUnreadAsync(
        Guid recipientUserId,
        CancellationToken cancellationToken = default);

    Task<bool> MarkReadAsync(
        Guid notificationId,
        Guid recipientUserId,
        CancellationToken cancellationToken = default);

    Task MarkAllReadAsync(
        Guid recipientUserId,
        CancellationToken cancellationToken = default);
}
