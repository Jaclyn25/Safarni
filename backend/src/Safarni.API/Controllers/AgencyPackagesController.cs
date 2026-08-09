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
[Route("api/agency/packages")]
public sealed class AgencyPackagesController : ControllerBase
{
    private readonly ISender _sender;

    public AgencyPackagesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new ListAgencyPackagesQuery(User.GetRequiredUserId()),
            cancellationToken);
        return result.IsFailure
            ? Failure<IReadOnlyList<TravelPackageResponse>>(result.Error, "Failed to load packages.")
            : Ok(ApiResponse<IReadOnlyList<TravelPackageResponse>>.SuccessResponse(
                result.Value!, "Packages loaded.", StatusCodes.Status200OK));
    }

    [HttpGet("{packageId:guid}")]
    public Task<IActionResult> Get(Guid packageId, CancellationToken cancellationToken) => Send(
        new GetAgencyPackageQuery(User.GetRequiredUserId(), packageId),
        "Package loaded.",
        cancellationToken);

    [HttpPost]
    public Task<IActionResult> Create([FromBody] TravelPackageRequest request, CancellationToken cancellationToken) => Send(
        new CreateTravelPackageCommand(User.GetRequiredUserId(), request.Title, request.Description,
            request.Destination, request.StartDate, request.EndDate, request.DurationDays, request.Price,
            request.CurrencyCode, request.AvailableSeats, request.CoverImageUrl, request.DiscountPercentage,
            request.OfferLabel),
        "Package created.", cancellationToken, StatusCodes.Status201Created);

    [HttpPut("{packageId:guid}")]
    public Task<IActionResult> Update(Guid packageId, [FromBody] TravelPackageRequest request, CancellationToken cancellationToken) => Send(
        new UpdateTravelPackageCommand(User.GetRequiredUserId(), packageId, request.Title, request.Description,
            request.Destination, request.StartDate, request.EndDate, request.DurationDays, request.Price,
            request.CurrencyCode, request.AvailableSeats, request.CoverImageUrl, request.DiscountPercentage,
            request.OfferLabel),
        "Package updated.", cancellationToken);

    [HttpPost("{packageId:guid}/publish")]
    public Task<IActionResult> Publish(Guid packageId, CancellationToken cancellationToken) => Send(
        new PublishTravelPackageCommand(User.GetRequiredUserId(), packageId), "Package published.", cancellationToken);

    [HttpPost("{packageId:guid}/unpublish")]
    public Task<IActionResult> Unpublish(Guid packageId, CancellationToken cancellationToken) => Send(
        new UnpublishTravelPackageCommand(User.GetRequiredUserId(), packageId), "Package unpublished.", cancellationToken);

    [HttpPost("{packageId:guid}/deactivate")]
    public Task<IActionResult> Deactivate(Guid packageId, CancellationToken cancellationToken) => Send(
        new DeactivateTravelPackageCommand(User.GetRequiredUserId(), packageId), "Package deactivated.", cancellationToken);

    [HttpPost("{packageId:guid}/days")]
    public Task<IActionResult> AddDay(
        Guid packageId,
        [FromBody] TravelPackageDayRequest request,
        CancellationToken cancellationToken) => Send(
        new AddTravelPackageDayCommand(User.GetRequiredUserId(), packageId, request.DayNumber,
            request.Title, request.Description, request.Activities),
        "Package day added.", cancellationToken);

    [HttpPut("{packageId:guid}/days/{dayId:guid}")]
    public Task<IActionResult> UpdateDay(
        Guid packageId,
        Guid dayId,
        [FromBody] TravelPackageDayRequest request,
        CancellationToken cancellationToken) => Send(
        new UpdateTravelPackageDayCommand(User.GetRequiredUserId(), packageId, dayId,
            request.DayNumber, request.Title, request.Description, request.Activities),
        "Package day updated.", cancellationToken);

    [HttpDelete("{packageId:guid}/days/{dayId:guid}")]
    public Task<IActionResult> DeleteDay(Guid packageId, Guid dayId, CancellationToken cancellationToken) => Send(
        new DeleteTravelPackageDayCommand(User.GetRequiredUserId(), packageId, dayId),
        "Package day removed.", cancellationToken);

    [HttpPost("{packageId:guid}/image")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 6 * 1024 * 1024)]
    public async Task<IActionResult> UploadImage(
        Guid packageId,
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file is null)
            return Failure<TravelPackageResponse>(TravelPackageErrors.InvalidPackageImage, "Package image upload failed.");

        await using var memory = new MemoryStream();
        await file.CopyToAsync(memory, cancellationToken);
        var result = await _sender.Send(
            new UploadTravelPackageImageCommand(
                User.GetRequiredUserId(), packageId, file.FileName, file.ContentType, file.Length, memory.ToArray()),
            cancellationToken);
        return result.IsFailure
            ? Failure<TravelPackageResponse>(result.Error, "Package image upload failed.")
            : Ok(ApiResponse<TravelPackageResponse>.SuccessResponse(
                result.Value!, "Package image uploaded.", StatusCodes.Status200OK));
    }

    private async Task<IActionResult> Send<TRequest>(TRequest request, string message, CancellationToken cancellationToken, int successStatus = StatusCodes.Status200OK)
        where TRequest : MediatR.IRequest<Safarni.Application.Common.Results.Result<TravelPackageResponse>>
    {
        var result = await _sender.Send(request, cancellationToken);
        if (result.IsFailure) return Failure<TravelPackageResponse>(result.Error, message);
        return StatusCode(successStatus, ApiResponse<TravelPackageResponse>.SuccessResponse(result.Value!, message, successStatus));
    }

    private ObjectResult Failure<T>(Error error, string message)
    {
        var status = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError
        };
        return StatusCode(status, ApiResponse<T>.FailureResponse([new ApiError(error.Code, error.Message)], message, status));
    }

    public sealed record TravelPackageRequest(
        string Title,
        string Description,
        string Destination,
        DateTime StartDate,
        DateTime EndDate,
        int DurationDays,
        decimal Price,
        string CurrencyCode,
        int AvailableSeats,
        string? CoverImageUrl,
        decimal? DiscountPercentage,
        string? OfferLabel);

    public sealed record TravelPackageDayRequest(
        int DayNumber,
        string Title,
        string Description,
        string? Activities);
}
