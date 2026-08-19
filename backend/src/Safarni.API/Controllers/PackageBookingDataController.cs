using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Safarni.API.Common;
using Safarni.API.Extensions;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Abstractions.Storage;
using Safarni.Application.Common.Errors;
using Safarni.Application.Features.Packages;

namespace Safarni.API.Controllers;

[ApiController]
[Authorize]
[Route("api/booking-requests/{requestId:guid}")]
public sealed class PackageBookingDataController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IPackageBookingRequestRepository _requests;
    private readonly IBookingDocumentStorage _storage;

    public PackageBookingDataController(ISender sender, IPackageBookingRequestRepository requests, IBookingDocumentStorage storage)
    { _sender = sender; _requests = requests; _storage = storage; }

    [HttpGet("travelers")]
    public async Task<IActionResult> ListTravelers(Guid requestId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListBookingTravelersQuery(User.GetRequiredUserId(), requestId), cancellationToken);
        return result.IsFailure
            ? Failure<IReadOnlyList<BookingTravelerResponse>>(result.Error, "Failed to load traveler data.")
            : Ok(ApiResponse<IReadOnlyList<BookingTravelerResponse>>.SuccessResponse(result.Value!, "Traveler data loaded.", StatusCodes.Status200OK));
    }

    [HttpPost("travelers")]
    public async Task<IActionResult> SaveTraveler(Guid requestId, [FromBody] SaveTravelerRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new UpsertBookingTravelerCommand(
            User.GetRequiredUserId(), requestId, request.TravelerId, request.FullName,
            request.DateOfBirth, request.Nationality, request.PassportNumber,
            request.PassportExpiry, request.Email, request.Phone), cancellationToken);
        return result.IsFailure
            ? Failure<BookingTravelerResponse>(result.Error, "Failed to save traveler data.")
            : Ok(ApiResponse<BookingTravelerResponse>.SuccessResponse(result.Value!, "Traveler data saved.", StatusCodes.Status200OK));
    }

    [HttpGet("documents")]
    public async Task<IActionResult> ListDocuments(Guid requestId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListBookingDocumentsQuery(User.GetRequiredUserId(), requestId), cancellationToken);
        return result.IsFailure
            ? Failure<IReadOnlyList<BookingDocumentResponse>>(result.Error, "Failed to load booking documents.")
            : Ok(ApiResponse<IReadOnlyList<BookingDocumentResponse>>.SuccessResponse(result.Value!, "Booking documents loaded.", StatusCodes.Status200OK));
    }

    [HttpPost("documents")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> UploadDocument(
        Guid requestId,
        [FromForm] UploadDocumentRequest request,
        CancellationToken cancellationToken)
    {
        if (request.File is null) return Failure<BookingDocumentResponse>(PackageBookingErrors.PaymentNotAllowed, "A document file is required.");
        await using var stream = request.File.OpenReadStream();
        var result = await _sender.Send(new UploadBookingDocumentCommand(
            User.GetRequiredUserId(), requestId, request.TravelerId, request.DocumentType,
            stream, request.File.FileName, request.File.ContentType, request.File.Length), cancellationToken);
        return result.IsFailure
            ? Failure<BookingDocumentResponse>(result.Error, "Failed to upload booking document.")
            : Ok(ApiResponse<BookingDocumentResponse>.SuccessResponse(result.Value!, "Booking document uploaded.", StatusCodes.Status201Created));
    }

    [HttpGet("documents/{documentId:guid}")]
    public async Task<IActionResult> DownloadDocument(Guid requestId, Guid documentId, CancellationToken cancellationToken)
    {
        var documents = await _sender.Send(new ListBookingDocumentsQuery(User.GetRequiredUserId(), requestId), cancellationToken);
        var summary = documents.Value?.FirstOrDefault(document => document.Id == documentId);
        if (documents.IsFailure || summary is null) return NotFound();
        var document = await _requests.GetDocumentAsync(requestId, documentId, cancellationToken);
        if (document is null) return NotFound();
        var stream = await _storage.OpenReadAsync(document.StorageReference, cancellationToken);
        return stream is null ? NotFound() : File(stream, document.ContentType, document.OriginalFileName);
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

    public sealed record SaveTravelerRequest(
        Guid? TravelerId,
        string FullName,
        DateTime? DateOfBirth,
        string Nationality,
        string PassportNumber,
        DateTime? PassportExpiry,
        string Email,
        string Phone);

    public sealed record UploadDocumentRequest(IFormFile? File, Guid? TravelerId, string DocumentType);
}
