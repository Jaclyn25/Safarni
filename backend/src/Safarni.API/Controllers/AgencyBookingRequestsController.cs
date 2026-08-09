using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Safarni.API.Common;
using Safarni.API.Extensions;
using Safarni.Application.Common.Errors;
using Safarni.Application.Features.Packages;

namespace Safarni.API.Controllers;

[ApiController]
[Authorize(Policy = AccountPolicies.TravelAgencyOnly)]
[Route("api/agency/booking-requests")]
public sealed class AgencyBookingRequestsController : ControllerBase
{
    private readonly ISender _sender;

    public AgencyBookingRequestsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new ListAgencyBookingRequestsQuery(User.GetRequiredUserId()), cancellationToken);
        return result.IsFailure
            ? Failure<IReadOnlyList<PackageBookingRequestResponse>>(result.Error, "Failed to load booking requests.")
            : Ok(ApiResponse<IReadOnlyList<PackageBookingRequestResponse>>.SuccessResponse(
                result.Value!, "Booking requests loaded.", StatusCodes.Status200OK));
    }

    [HttpGet("{requestId:guid}")]
    public async Task<IActionResult> Get(Guid requestId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new GetAgencyBookingRequestQuery(User.GetRequiredUserId(), requestId), cancellationToken);
        return result.IsFailure
            ? Failure<PackageBookingRequestResponse>(result.Error, "Failed to load booking request.")
            : Ok(ApiResponse<PackageBookingRequestResponse>.SuccessResponse(
                result.Value!, "Booking request loaded.", StatusCodes.Status200OK));
    }

    [HttpPost("{requestId:guid}/accept")]
    public Task<IActionResult> Accept(Guid requestId, [FromBody] ProcessBookingRequest request, CancellationToken cancellationToken) =>
        Process(new AcceptPackageBookingRequestCommand(User.GetRequiredUserId(), requestId, request.Response), "Booking request accepted.", cancellationToken);

    [HttpPost("{requestId:guid}/reject")]
    public Task<IActionResult> Reject(Guid requestId, [FromBody] ProcessBookingRequest request, CancellationToken cancellationToken) =>
        Process(new RejectPackageBookingRequestCommand(User.GetRequiredUserId(), requestId, request.Response), "Booking request rejected.", cancellationToken);

    private async Task<IActionResult> Process<TCommand>(TCommand command, string message, CancellationToken cancellationToken)
        where TCommand : IRequest<Safarni.Application.Common.Results.Result<PackageBookingRequestResponse>>
    {
        var result = await _sender.Send(command, cancellationToken);
        return result.IsFailure
            ? Failure<PackageBookingRequestResponse>(result.Error, "Failed to process booking request.")
            : Ok(ApiResponse<PackageBookingRequestResponse>.SuccessResponse(
                result.Value!, message, StatusCodes.Status200OK));
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

    public sealed record ProcessBookingRequest(string? Response);
}
