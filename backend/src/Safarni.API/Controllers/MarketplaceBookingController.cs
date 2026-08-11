using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Safarni.API.Common;
using Safarni.API.Extensions;
using Safarni.Application.Common.Errors;
using Safarni.Application.Features.Packages;

namespace Safarni.API.Controllers;

[ApiController]
[Authorize(Policy = AccountPolicies.TravelerOnly)]
[Route("api/marketplace/packages/{packageId:guid}/booking-requests")]
public sealed class MarketplaceBookingController : ControllerBase
{
    private readonly ISender _sender;

    public MarketplaceBookingController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        Guid packageId,
        [FromBody] CreateBookingRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new CreatePackageBookingRequestCommand(
                User.GetRequiredUserId(), packageId, request.TravelerCount, request.Message,
                request.AdultCount, request.ChildrenAges, request.ContactPhone, request.AlternatePhone,
                request.Country, request.City),
            cancellationToken);
        if (result.IsFailure) return Failure<PackageBookingRequestResponse>(result.Error, "Booking request failed.");
        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<PackageBookingRequestResponse>.SuccessResponse(
                result.Value!, "Booking request submitted.", StatusCodes.Status201Created));
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

    public sealed record CreateBookingRequest(
        int TravelerCount,
        string? Message,
        int AdultCount,
        IReadOnlyList<int> ChildrenAges,
        string ContactPhone,
        string? AlternatePhone,
        string Country,
        string City);
}
