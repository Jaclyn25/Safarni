using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Safarni.API.Common;
using Safarni.API.Extensions;
using Safarni.Application.Abstractions.Payments;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Errors;
using Safarni.Application.Features.Packages;
using Safarni.Domain.Enums;

namespace Safarni.API.Controllers;

[ApiController]
[Authorize(Policy = AccountPolicies.TravelerOnly)]
[Route("api/booking-requests")]
public sealed class PackageBookingRequestsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IPaymentGateway _paymentGateway;
    private readonly IPackageBookingRequestRepository _requests;

    public PackageBookingRequestsController(
        ISender sender,
        IPaymentGateway paymentGateway,
        IPackageBookingRequestRepository requests)
    {
        _sender = sender;
        _paymentGateway = paymentGateway;
        _requests = requests;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new ListTravelerBookingRequestsQuery(User.GetRequiredUserId()), cancellationToken);
        return result.IsFailure
            ? Failure<IReadOnlyList<PackageBookingRequestResponse>>(result.Error, "Failed to load booking requests.")
            : Ok(ApiResponse<IReadOnlyList<PackageBookingRequestResponse>>.SuccessResponse(
                result.Value!, "Booking requests loaded.", StatusCodes.Status200OK));
    }

    [HttpGet("{requestId:guid}")]
    public async Task<IActionResult> Get(Guid requestId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new GetTravelerBookingRequestQuery(User.GetRequiredUserId(), requestId), cancellationToken);
        return result.IsFailure
            ? Failure<PackageBookingRequestResponse>(result.Error, "Failed to load booking request.")
            : Ok(ApiResponse<PackageBookingRequestResponse>.SuccessResponse(
                result.Value!, "Booking request loaded.", StatusCodes.Status200OK));
    }

    [HttpPost("{requestId:guid}/offers/{offerId:guid}/accept")]
    public async Task<IActionResult> AcceptOffer(
        Guid requestId,
        Guid offerId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new AcceptPackageBookingOfferCommand(
                User.GetRequiredUserId(),
                requestId,
                offerId),
            cancellationToken);
        return result.IsFailure
            ? Failure<PackageBookingRequestResponse>(result.Error, "Failed to accept booking offer.")
            : Ok(ApiResponse<PackageBookingRequestResponse>.SuccessResponse(
                result.Value!, "Booking offer accepted. Payment is required to confirm the booking.", StatusCodes.Status200OK));
    }

    [HttpPost("{requestId:guid}/payment")]
    public async Task<IActionResult> StartPayment(Guid requestId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new CreateBookingPaymentCommand(User.GetRequiredUserId(), requestId), cancellationToken);
        return result.IsFailure
            ? Failure<BookingPaymentResponse>(result.Error, "Failed to create booking payment.")
            : Ok(ApiResponse<BookingPaymentResponse>.SuccessResponse(
                result.Value!, "Booking payment checkout created.", StatusCodes.Status201Created));
    }

    [HttpPost("{requestId:guid}/refund")]
    public async Task<IActionResult> Refund(
        Guid requestId,
        [FromBody] RefundRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new RequestBookingRefundCommand(User.GetRequiredUserId(), requestId, request.Reason), cancellationToken);
        return result.IsFailure
            ? Failure<BookingPaymentResponse>(result.Error, "Failed to refund booking payment.")
            : Ok(ApiResponse<BookingPaymentResponse>.SuccessResponse(
                result.Value!, "Booking payment refunded.", StatusCodes.Status200OK));
    }

    [HttpPost("{requestId:guid}/disputes")]
    public async Task<IActionResult> OpenDispute(Guid requestId, [FromBody] DisputeRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new OpenBookingDisputeCommand(User.GetRequiredUserId(), requestId, request.Reason, request.RequestedRefundAmount), cancellationToken);
        return result.IsFailure
            ? Failure<BookingDisputeResponse>(result.Error, "Booking dispute could not be opened.")
            : Ok(ApiResponse<BookingDisputeResponse>.SuccessResponse(result.Value!, "Booking dispute opened for review.", StatusCodes.Status201Created));
    }

    [AllowAnonymous]
    [HttpPost("~/api/booking-payments/webhooks/test")]
    public Task<IActionResult> TestWebhook(
        [FromHeader(Name = "X-Payment-Signature")] string signature,
        CancellationToken cancellationToken) => ProcessWebhook(signature, cancellationToken);

    [AllowAnonymous]
    [HttpPost("~/api/booking-payments/webhooks/stripe")]
    public Task<IActionResult> StripeWebhook(
        [FromHeader(Name = "Stripe-Signature")] string signature,
        CancellationToken cancellationToken) => ProcessWebhook(signature, cancellationToken);

    [HttpPost("~/api/booking-payments/test/complete/{sessionId}")]
    public async Task<IActionResult> CompleteTestPayment(
        string sessionId,
        [FromQuery] bool success = true,
        CancellationToken cancellationToken = default)
    {
        var payment = await _requests.GetPaymentBySessionAsync("Test", sessionId, cancellationToken);
        if (payment is null || payment.PackageBookingRequest.TravelerUserId != User.GetRequiredUserId())
            return Failure<BookingPaymentResponse>(PackageBookingErrors.PaymentNotFound, "Booking payment not found.");

        var result = await _sender.Send(new ProcessBookingPaymentWebhookCommand(new PaymentWebhookEvent(
            "Test",
            $"test_booking_event_{Guid.NewGuid():N}",
            sessionId,
            success ? PaymentStatus.Succeeded : PaymentStatus.Failed,
            success ? null : "The test booking payment was declined.",
            DateTime.UtcNow)), cancellationToken);
        return result.IsFailure
            ? Failure<BookingPaymentResponse>(result.Error, "Booking payment could not be processed.")
            : Ok(ApiResponse<BookingPaymentResponse>.SuccessResponse(
                result.Value!, success ? "Booking payment completed." : "Booking payment failed.", StatusCodes.Status200OK));
    }

    public sealed record RefundRequest(string Reason);
    public sealed record DisputeRequest(string Reason, decimal? RequestedRefundAmount);

    private async Task<IActionResult> ProcessWebhook(string signature, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync(cancellationToken);
        PaymentWebhookEvent? webhook;
        try
        {
            webhook = await _paymentGateway.VerifyWebhookAsync(payload, signature, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return Failure<object>(PackageBookingErrors.PaymentNotFound, "Booking payment webhook rejected.");
        }

        if (webhook is null)
            return Ok(ApiResponse<object>.SuccessResponse(new { ignored = true }, "Booking payment event acknowledged.", StatusCodes.Status200OK));

        var result = await _sender.Send(new ProcessBookingPaymentWebhookCommand(webhook), cancellationToken);
        return result.IsFailure
            ? Failure<BookingPaymentResponse>(result.Error, "Booking payment webhook failed.")
            : Ok(ApiResponse<BookingPaymentResponse>.SuccessResponse(result.Value!, "Booking payment webhook processed.", StatusCodes.Status200OK));
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
}
