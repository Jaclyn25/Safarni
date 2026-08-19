using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Safarni.API.Common;
using Safarni.API.Extensions;
using Safarni.Application.Common.Errors;
using Safarni.Application.Features.Packages;

namespace Safarni.API.Controllers;

[ApiController]
[Authorize]
[Route("api/booking-requests/{requestId:guid}/messages")]
public sealed class PackageBookingMessagesController : ControllerBase
{
    private readonly ISender _sender;

    public PackageBookingMessagesController(ISender sender) => _sender = sender;

    [HttpGet]
    public async Task<IActionResult> List(Guid requestId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListBookingMessagesQuery(User.GetRequiredUserId(), requestId), cancellationToken);
        return result.IsFailure
            ? Failure<IReadOnlyList<PackageBookingMessageResponse>>(result.Error, "Failed to load booking messages.")
            : Ok(ApiResponse<IReadOnlyList<PackageBookingMessageResponse>>.SuccessResponse(
                result.Value!, "Booking messages loaded.", StatusCodes.Status200OK));
    }

    [HttpPost]
    public async Task<IActionResult> Send(Guid requestId, [FromBody] SendMessageRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new SendBookingMessageCommand(User.GetRequiredUserId(), requestId, request.Body), cancellationToken);
        return result.IsFailure
            ? Failure<PackageBookingMessageResponse>(result.Error, "Failed to send booking message.")
            : Ok(ApiResponse<PackageBookingMessageResponse>.SuccessResponse(
                result.Value!, "Booking message sent.", StatusCodes.Status201Created));
    }

    private ObjectResult Failure<T>(Error error, string message)
    {
        var status = error.Type switch
        {
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };
        return StatusCode(status, ApiResponse<T>.FailureResponse([new ApiError(error.Code, error.Message)], message, status));
    }

    public sealed record SendMessageRequest(string Body);
}
