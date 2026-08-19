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
[Route("api/agency/booking-requests/{requestId:guid}/supplier-evidence")]
public sealed class AgencySupplierEvidenceController : ControllerBase
{
    private readonly ISender _sender;
    public AgencySupplierEvidenceController(ISender sender) => _sender = sender;

    [HttpPost]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> Submit(Guid requestId, [FromForm] SupplierEvidenceRequest request, CancellationToken cancellationToken)
    {
        if (request.File is null) return Failure<SupplierEvidenceResponse>(PackageBookingErrors.SupplierEvidenceNotAllowed, "A supplier proof file is required.");
        await using var stream = request.File.OpenReadStream();
        var result = await _sender.Send(new SubmitSupplierEvidenceCommand(
            User.GetRequiredUserId(), requestId, request.SupplierType, request.SupplierName,
            request.ConfirmationCode, request.InvoiceAmount, request.CurrencyCode, stream,
            request.File.FileName, request.File.ContentType, request.File.Length), cancellationToken);
        return result.IsFailure
            ? Failure<SupplierEvidenceResponse>(result.Error, "Supplier evidence could not be submitted.")
            : Ok(ApiResponse<SupplierEvidenceResponse>.SuccessResponse(result.Value!, "Supplier evidence submitted for review.", StatusCodes.Status201Created));
    }

    private ObjectResult Failure<T>(Error error, string message) => StatusCode(
        error.Type switch { ErrorType.NotFound => 404, ErrorType.Forbidden => 403, ErrorType.Conflict => 409, _ => 400 },
        ApiResponse<T>.FailureResponse([new ApiError(error.Code, error.Message)], message, error.Type switch { ErrorType.NotFound => 404, ErrorType.Forbidden => 403, ErrorType.Conflict => 409, _ => 400 }));

    public sealed record SupplierEvidenceRequest(
        IFormFile? File,
        string SupplierType,
        string SupplierName,
        string ConfirmationCode,
        decimal InvoiceAmount,
        string CurrencyCode);
}
