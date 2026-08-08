using MediatR;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Results;
using Safarni.Domain.Entities;
using Safarni.Domain.Enums;

namespace Safarni.Application.Features.Notifications;

public sealed record UserNotificationResponse(
    Guid Id,
    MarketplaceNotificationType Type,
    string Title,
    string Message,
    string? ActionUrl,
    bool IsRead,
    DateTime CreatedAt,
    DateTime? ReadAt);

public sealed record UserNotificationListResponse(
    int UnreadCount,
    IReadOnlyList<UserNotificationResponse> Items);

public sealed record GetMyNotificationsQuery(Guid UserId, int Take)
    : IRequest<Result<UserNotificationListResponse>>;

public sealed class GetMyNotificationsQueryHandler
    : IRequestHandler<GetMyNotificationsQuery, Result<UserNotificationListResponse>>
{
    private readonly IUserNotificationRepository _notifications;

    public GetMyNotificationsQueryHandler(IUserNotificationRepository notifications)
    {
        _notifications = notifications;
    }

    public async Task<Result<UserNotificationListResponse>> Handle(
        GetMyNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        var take = Math.Clamp(request.Take, 1, 50);
        var items = await _notifications.ListAsync(request.UserId, take, cancellationToken);
        var unreadCount = await _notifications.CountUnreadAsync(request.UserId, cancellationToken);
        return Result<UserNotificationListResponse>.Success(
            new UserNotificationListResponse(unreadCount, items.Select(Map).ToArray()));
    }

    private static UserNotificationResponse Map(UserNotification notification) =>
        new(
            notification.Id,
            notification.Type,
            notification.Title,
            notification.Message,
            notification.ActionUrl,
            notification.IsRead,
            notification.CreatedAt,
            notification.ReadAt);
}

public sealed record MarkNotificationReadCommand(Guid UserId, Guid NotificationId)
    : IRequest<Result<bool>>;

public sealed class MarkNotificationReadCommandHandler
    : IRequestHandler<MarkNotificationReadCommand, Result<bool>>
{
    private readonly IUserNotificationRepository _notifications;

    public MarkNotificationReadCommandHandler(IUserNotificationRepository notifications)
    {
        _notifications = notifications;
    }

    public async Task<Result<bool>> Handle(
        MarkNotificationReadCommand request,
        CancellationToken cancellationToken)
    {
        var updated = await _notifications.MarkReadAsync(
            request.NotificationId,
            request.UserId,
            cancellationToken);
        return Result<bool>.Success(updated);
    }
}

public sealed record MarkAllNotificationsReadCommand(Guid UserId)
    : IRequest<Result<bool>>;

public sealed class MarkAllNotificationsReadCommandHandler
    : IRequestHandler<MarkAllNotificationsReadCommand, Result<bool>>
{
    private readonly IUserNotificationRepository _notifications;

    public MarkAllNotificationsReadCommandHandler(IUserNotificationRepository notifications)
    {
        _notifications = notifications;
    }

    public async Task<Result<bool>> Handle(
        MarkAllNotificationsReadCommand request,
        CancellationToken cancellationToken)
    {
        await _notifications.MarkAllReadAsync(request.UserId, cancellationToken);
        return Result<bool>.Success(true);
    }
}
