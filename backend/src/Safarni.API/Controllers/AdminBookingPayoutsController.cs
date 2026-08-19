using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Safarni.API.Common;
using Safarni.API.Extensions;
using Safarni.Application.Common.Errors;
using Safarni.Application.Features.Packages;
using Safarni.Application.Features.Agencies.Payout;
using Safarni.Domain.Enums;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Abstractions.Storage;

namespace Safarni.API.Controllers;

[ApiController]
[Authorize(Policy = AdminPolicies.AdminOnly)]
[Route("api/admin/booking-payouts")]
public sealed class AdminBookingPayoutsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IPackageBookingRequestRepository _requests;
    private readonly IBookingDocumentStorage _storage;
    public AdminBookingPayoutsController(ISender sender, IPackageBookingRequestRepository requests, IBookingDocumentStorage storage)
    { _sender = sender; _requests = requests; _storage = storage; }

    [HttpGet("pending-evidence")]
    public async Task<IActionResult> PendingEvidence(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListPendingSupplierEvidenceQuery(User.GetRequiredUserId()), cancellationToken);
        return result.IsFailure
            ? Failure<IReadOnlyList<SupplierEvidenceResponse>>(result.Error, "Failed to load supplier evidence.")
            : Ok(ApiResponse<IReadOnlyList<SupplierEvidenceResponse>>.SuccessResponse(result.Value!, "Supplier evidence loaded.", StatusCodes.Status200OK));
    }

    [HttpGet("payout-profiles")]
    public async Task<IActionResult> PayoutProfiles([FromQuery] AgencyPayoutProfileStatus? status, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListAgencyPayoutProfilesQuery(User.GetRequiredUserId(), status), cancellationToken);
        return result.IsFailure
            ? Failure<IReadOnlyList<AdminAgencyPayoutProfileResponse>>(result.Error, "Failed to load agency payout profiles.")
            : Ok(ApiResponse<IReadOnlyList<AdminAgencyPayoutProfileResponse>>.SuccessResponse(result.Value!, "Agency payout profiles loaded.", StatusCodes.Status200OK));
    }

    [HttpGet("payouts")]
    public async Task<IActionResult> Payouts(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListActionableBookingPayoutsQuery(User.GetRequiredUserId()), cancellationToken);
        return result.IsFailure
            ? Failure<IReadOnlyList<BookingPayoutResponse>>(result.Error, "Failed to load actionable booking payouts.")
            : Ok(ApiResponse<IReadOnlyList<BookingPayoutResponse>>.SuccessResponse(result.Value!, "Actionable booking payouts loaded.", StatusCodes.Status200OK));
    }

    [HttpPost("payout-profiles/{agencyId:guid}/review")]
    public async Task<IActionResult> ReviewPayoutProfile(Guid agencyId, [FromBody] PayoutProfileReviewRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ReviewAgencyPayoutProfileCommand(User.GetRequiredUserId(), agencyId, request.Approve, request.ReviewNote), cancellationToken);
        return result.IsFailure
            ? Failure<AdminAgencyPayoutProfileResponse>(result.Error, "Agency payout profile review failed.")
            : Ok(ApiResponse<AdminAgencyPayoutProfileResponse>.SuccessResponse(result.Value!, "Agency payout profile review saved.", StatusCodes.Status200OK));
    }

    [HttpPost("{evidenceId:guid}/review")]
    public async Task<IActionResult> Review(Guid evidenceId, [FromBody] ReviewRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ReviewSupplierEvidenceCommand(User.GetRequiredUserId(), evidenceId, request.Approve, request.ReviewNote), cancellationToken);
        return result.IsFailure
            ? Failure<SupplierEvidenceResponse>(result.Error, "Supplier evidence review failed.")
            : Ok(ApiResponse<SupplierEvidenceResponse>.SuccessResponse(result.Value!, "Supplier evidence review saved.", StatusCodes.Status200OK));
    }

    [HttpPost("{evidenceId:guid}/paid")]
    public async Task<IActionResult> Paid(Guid evidenceId, [FromBody] PaidRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new MarkBookingPayoutPaidCommand(User.GetRequiredUserId(), evidenceId, request.ExternalTransferId), cancellationToken);
        return result.IsFailure
            ? Failure<SupplierEvidenceResponse>(result.Error, "Booking payout could not be marked as paid.")
            : Ok(ApiResponse<SupplierEvidenceResponse>.SuccessResponse(result.Value!, "Booking payout marked as paid.", StatusCodes.Status200OK));
    }

    [HttpPost("payout/{payoutId:guid}/paid")]
    public async Task<IActionResult> PaidPayout(Guid payoutId, [FromBody] PaidRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new MarkBookingPayoutPaidByIdCommand(User.GetRequiredUserId(), payoutId, request.ExternalTransferId), cancellationToken);
        return result.IsFailure
            ? Failure<BookingPayoutResponse>(result.Error, "Booking payout could not be marked as paid.")
            : Ok(ApiResponse<BookingPayoutResponse>.SuccessResponse(result.Value!, "Booking payout marked as paid.", StatusCodes.Status200OK));
    }

    [HttpGet("{evidenceId:guid}/file")]
    public async Task<IActionResult> EvidenceFile(Guid evidenceId, CancellationToken cancellationToken)
    {
        var evidence = await _requests.GetEvidenceAsync(evidenceId, cancellationToken);
        if (evidence is null) return NotFound();
        var stream = await _storage.OpenReadAsync(evidence.StorageReference, cancellationToken);
        return stream is null ? NotFound() : File(stream, evidence.ContentType, evidence.OriginalFileName);
    }

    private ObjectResult Failure<T>(Error error, string message)
    {
        var status = error.Type switch { ErrorType.NotFound => 404, ErrorType.Forbidden => 403, ErrorType.Conflict => 409, _ => 400 };
        return StatusCode(status, ApiResponse<T>.FailureResponse([new ApiError(error.Code, error.Message)], message, status));
    }

    public sealed record ReviewRequest(bool Approve, string? ReviewNote);
    public sealed record PaidRequest(string ExternalTransferId);
    public sealed record PayoutProfileReviewRequest(bool Approve, string? ReviewNote);
}
