using MediatR;
using Microsoft.AspNetCore.Mvc;
using Safarni.API.Common;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;
using Safarni.Application.Features.Packages;

namespace Safarni.API.Controllers;

[ApiController]
[Route("api/marketplace/packages")]
public sealed class MarketplacePackagesController : ControllerBase
{
    private readonly ISender _sender;

    public MarketplacePackagesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? destination,
        [FromQuery] DateTime? startDate,
        [FromQuery] int? durationDays,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new ListPublishedPackagesQuery(
            destination, startDate, durationDays, minPrice, maxPrice, page, pageSize), cancellationToken);
        return result.IsFailure
            ? Failure<PagedResult<TravelPackageResponse>>(result.Error, "Failed to load marketplace packages.")
            : Ok(ApiResponse<PagedResult<TravelPackageResponse>>.SuccessResponse(result.Value!, "Marketplace packages loaded.", StatusCodes.Status200OK));
    }

    [HttpGet("{packageId:guid}")]
    public async Task<IActionResult> Get(Guid packageId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetPublishedPackageQuery(packageId), cancellationToken);
        return result.IsFailure
            ? Failure<TravelPackageResponse>(result.Error, "Failed to load marketplace package.")
            : Ok(ApiResponse<TravelPackageResponse>.SuccessResponse(result.Value!, "Marketplace package loaded.", StatusCodes.Status200OK));
    }

    private ObjectResult Failure<T>(Error error, string message)
    {
        var status = error.Type == ErrorType.NotFound ? StatusCodes.Status404NotFound : StatusCodes.Status400BadRequest;
        return StatusCode(status, ApiResponse<T>.FailureResponse([new ApiError(error.Code, error.Message)], message, status));
    }
}
