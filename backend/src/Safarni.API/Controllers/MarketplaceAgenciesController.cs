using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Safarni.API.Common;
using Safarni.API.Extensions;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;
using Safarni.Application.Features.Agencies.Public;

namespace Safarni.API.Controllers;

[ApiController]
[Route("api/marketplace/agencies")]
public sealed class MarketplaceAgenciesController : ControllerBase
{
    private readonly ISender _sender;
    public MarketplaceAgenciesController(ISender sender) => _sender = sender;

    [HttpGet("{agencyId:guid}")]
    public async Task<IActionResult> Get(Guid agencyId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetPublicAgencyQuery(agencyId), cancellationToken);
        return Respond(result, "Agency profile loaded.");
    }

    [HttpGet("{agencyId:guid}/reviews")]
    public async Task<IActionResult> Reviews(Guid agencyId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new ListPublicAgencyReviewsQuery(agencyId, page, pageSize), cancellationToken);
        return Respond(result, "Agency reviews loaded.");
    }

    [Authorize(Policy = AccountPolicies.TravelerOnly)]
    [HttpPost("{agencyId:guid}/reviews")]
    public async Task<IActionResult> Review(Guid agencyId, [FromBody] CreateReviewRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new CreateAgencyReviewCommand(User.GetRequiredUserId(), agencyId,
            request.BookingRequestId, request.Rating, request.Comment), cancellationToken);
        return Respond(result, "Review submitted.", StatusCodes.Status201Created);
    }

    private IActionResult Respond<T>(Result<T> result, string message, int successStatus = StatusCodes.Status200OK)
    {
        if (result.IsSuccess)
            return StatusCode(successStatus, ApiResponse<T>.SuccessResponse(result.Value!, message, successStatus));
        var status = result.Error.Type switch
        {
            ErrorType.NotFound => 404,
            ErrorType.Forbidden => 403,
            ErrorType.Conflict => 409,
            _ => 400
        };
        return StatusCode(status, ApiResponse<T>.FailureResponse([new ApiError(result.Error.Code, result.Error.Message)], message, status));
    }

    public sealed record CreateReviewRequest(Guid BookingRequestId, int Rating, string Comment);
}
