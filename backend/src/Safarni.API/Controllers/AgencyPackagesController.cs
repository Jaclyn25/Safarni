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
            request.OfferLabel, request.Inclusions, request.Exclusions, request.ImportantNotes,
            request.CancellationPolicy),
        "Package created.", cancellationToken, StatusCodes.Status201Created);

    [HttpPut("{packageId:guid}")]
    public Task<IActionResult> Update(Guid packageId, [FromBody] TravelPackageRequest request, CancellationToken cancellationToken) => Send(
        new UpdateTravelPackageCommand(User.GetRequiredUserId(), packageId, request.Title, request.Description,
            request.Destination, request.StartDate, request.EndDate, request.DurationDays, request.Price,
            request.CurrencyCode, request.AvailableSeats, request.CoverImageUrl, request.DiscountPercentage,
            request.OfferLabel, request.Inclusions, request.Exclusions, request.ImportantNotes,
            request.CancellationPolicy),
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
            request.Title, request.Description),
        "Package day added.", cancellationToken);

    [HttpPut("{packageId:guid}/days/{dayId:guid}")]
    public Task<IActionResult> UpdateDay(
        Guid packageId,
        Guid dayId,
        [FromBody] TravelPackageDayRequest request,
        CancellationToken cancellationToken) => Send(
            new UpdateTravelPackageDayCommand(User.GetRequiredUserId(), packageId, dayId,
            request.DayNumber, request.Title, request.Description),
        "Package day updated.", cancellationToken);

    [HttpDelete("{packageId:guid}/days/{dayId:guid}")]
    public Task<IActionResult> DeleteDay(Guid packageId, Guid dayId, CancellationToken cancellationToken) => Send(
        new DeleteTravelPackageDayCommand(User.GetRequiredUserId(), packageId, dayId),
        "Package day removed.", cancellationToken);

    [HttpPost("{packageId:guid}/cities")]
    public Task<IActionResult> AddCity(Guid packageId, [FromBody] TravelPackageCityRequest request, CancellationToken cancellationToken) => Send(
        new SaveTravelPackageCityCommand(User.GetRequiredUserId(), packageId, null, request.Name,
            request.Nights, request.Description),
        "Package city added.", cancellationToken);

    [HttpPut("{packageId:guid}/cities/{cityId:guid}")]
    public Task<IActionResult> UpdateCity(Guid packageId, Guid cityId, [FromBody] TravelPackageCityRequest request, CancellationToken cancellationToken) => Send(
        new SaveTravelPackageCityCommand(User.GetRequiredUserId(), packageId, cityId, request.Name,
            request.Nights, request.Description),
        "Package city updated.", cancellationToken);

    [HttpDelete("{packageId:guid}/cities/{cityId:guid}")]
    public Task<IActionResult> DeleteCity(Guid packageId, Guid cityId, CancellationToken cancellationToken) => Send(
        new DeleteTravelPackageCityCommand(User.GetRequiredUserId(), packageId, cityId),
        "Package city removed.", cancellationToken);

    [HttpPost("{packageId:guid}/days/{dayId:guid}/items")]
    public Task<IActionResult> AddItem(Guid packageId, Guid dayId, [FromBody] TravelPackageItemInput request, CancellationToken cancellationToken) => Send(
        new SaveTravelPackageItemCommand(User.GetRequiredUserId(), packageId, dayId, null, request),
        "Itinerary item added.", cancellationToken);

    [HttpPut("{packageId:guid}/days/{dayId:guid}/items/{itemId:guid}")]
    public Task<IActionResult> UpdateItem(Guid packageId, Guid dayId, Guid itemId, [FromBody] TravelPackageItemInput request, CancellationToken cancellationToken) => Send(
        new SaveTravelPackageItemCommand(User.GetRequiredUserId(), packageId, dayId, itemId, request),
        "Itinerary item updated.", cancellationToken);

    [HttpDelete("{packageId:guid}/days/{dayId:guid}/items/{itemId:guid}")]
    public Task<IActionResult> DeleteItem(Guid packageId, Guid dayId, Guid itemId, CancellationToken cancellationToken) => Send(
        new DeleteTravelPackageItemCommand(User.GetRequiredUserId(), packageId, dayId, itemId),
        "Itinerary item removed.", cancellationToken);

    [HttpGet("{packageId:guid}/days/{dayId:guid}/place-search")]
    public async Task<IActionResult> SearchPlaces(
        Guid packageId,
        Guid dayId,
        [FromQuery] TravelPackageItemType type,
        [FromQuery] string query = "",
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new SearchPackagePlacesQuery(
            User.GetRequiredUserId(), packageId, dayId, query, type), cancellationToken);
        return result.IsFailure
            ? Failure<IReadOnlyList<PackagePlaceSearchResult>>(result.Error, "Place search failed.")
            : Ok(ApiResponse<IReadOnlyList<PackagePlaceSearchResult>>.SuccessResponse(result.Value!, "Places loaded.", StatusCodes.Status200OK));
    }

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

    [HttpPost("{packageId:guid}/images")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 6 * 1024 * 1024)]
    public async Task<IActionResult> UploadGalleryImage(
        Guid packageId,
        [FromForm] TravelPackageGalleryImageRequest request,
        CancellationToken cancellationToken)
    {
        if (request.File is null)
            return Failure<TravelPackageResponse>(TravelPackageErrors.InvalidPackageImage, "Image upload failed.");
        await using var memory = new MemoryStream();
        await request.File.CopyToAsync(memory, cancellationToken);
        var result = await _sender.Send(new UploadTravelPackageGalleryImageCommand(
            User.GetRequiredUserId(), packageId, request.DayId, request.ItemId, request.Caption, request.AltText,
            request.IsCover, request.File.FileName, request.File.ContentType, request.File.Length, memory.ToArray()), cancellationToken);
        return result.IsFailure
            ? Failure<TravelPackageResponse>(result.Error, "Image upload failed.")
            : Ok(ApiResponse<TravelPackageResponse>.SuccessResponse(result.Value!, "Image uploaded.", StatusCodes.Status200OK));
    }

    [HttpDelete("{packageId:guid}/images/{imageId:guid}")]
    public Task<IActionResult> DeleteGalleryImage(Guid packageId, Guid imageId, CancellationToken cancellationToken) => Send(
        new DeleteTravelPackageGalleryImageCommand(User.GetRequiredUserId(), packageId, imageId),
        "Image removed.", cancellationToken);

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
        string? OfferLabel,
        string? Inclusions,
        string? Exclusions,
        string? ImportantNotes,
        string? CancellationPolicy);

    public sealed record TravelPackageDayRequest(
        int DayNumber,
        string Title,
        string Description);

    public sealed record TravelPackageCityRequest(
        string Name,
        int Nights,
        string? Description);

    public sealed record TravelPackageGalleryImageRequest(
        Guid? DayId,
        Guid? ItemId,
        string? Caption,
        string? AltText,
        bool IsCover,
        IFormFile? File);
}
