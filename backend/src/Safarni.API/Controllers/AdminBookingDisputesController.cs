using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Safarni.API.Common;
using Safarni.API.Extensions;
using Safarni.Application.Common.Errors;
using Safarni.Application.Features.Packages;
using Safarni.Domain.Enums;

namespace Safarni.API.Controllers;

[ApiController]
[Authorize(Policy = AdminPolicies.AdminOnly)]
[Route("api/admin/booking-disputes")]
public sealed class AdminBookingDisputesController : ControllerBase
{
    private readonly ISender _sender;
    public AdminBookingDisputesController(ISender sender) => _sender = sender;

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListBookingDisputesQuery(User.GetRequiredUserId()), cancellationToken);
        return result.IsFailure
            ? Failure<IReadOnlyList<BookingDisputeResponse>>(result.Error, "Failed to load booking disputes.")
            : Ok(ApiResponse<IReadOnlyList<BookingDisputeResponse>>.SuccessResponse(result.Value!, "Booking disputes loaded.", StatusCodes.Status200OK));
    }

    [HttpPost("{disputeId:guid}/resolve")]
    public async Task<IActionResult> Resolve(Guid disputeId, [FromBody] ResolveRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ResolveBookingDisputeCommand(User.GetRequiredUserId(), disputeId, request.Decision, request.RefundAmount, request.AdminNote), cancellationToken);
        return result.IsFailure
            ? Failure<BookingDisputeResponse>(result.Error, "Booking dispute could not be resolved.")
            : Ok(ApiResponse<BookingDisputeResponse>.SuccessResponse(result.Value!, "Booking dispute resolved.", StatusCodes.Status200OK));
    }

    private ObjectResult Failure<T>(Error error, string message)
    {
        var status = error.Type switch { ErrorType.NotFound => 404, ErrorType.Forbidden => 403, ErrorType.Conflict => 409, _ => 400 };
        return StatusCode(status, ApiResponse<T>.FailureResponse([new ApiError(error.Code, error.Message)], message, status));
    }

    public sealed record ResolveRequest(BookingDisputeStatus Decision, decimal? RefundAmount, string? AdminNote);
}
