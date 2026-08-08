using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Safarni.API.Common;
using Safarni.API.Extensions;
using Safarni.Application.Features.Notifications;

namespace Safarni.API.Controllers;

[ApiController]
[Authorize]
[Route("api/notifications")]
public sealed class NotificationsController : ControllerBase
{
    private readonly ISender _sender;

    public NotificationsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new GetMyNotificationsQuery(User.GetRequiredUserId(), take),
            cancellationToken);
        return Ok(ApiResponse<UserNotificationListResponse>.SuccessResponse(
            result.Value!,
            "Notifications loaded.",
            StatusCodes.Status200OK));
    }

    [HttpPatch("{notificationId:guid}/read")]
    public async Task<IActionResult> MarkRead(
        Guid notificationId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new MarkNotificationReadCommand(
                User.GetRequiredUserId(),
                notificationId),
            cancellationToken);
        return Ok(ApiResponse<bool>.SuccessResponse(
            result.Value,
            "Notification updated.",
            StatusCodes.Status200OK));
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new MarkAllNotificationsReadCommand(User.GetRequiredUserId()),
            cancellationToken);
        return Ok(ApiResponse<bool>.SuccessResponse(
            result.Value,
            "Notifications marked as read.",
            StatusCodes.Status200OK));
    }
}
