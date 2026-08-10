using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Safarni.API.Common;
using Safarni.API.Extensions;
using Safarni.Application.Abstractions.Payments;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;
using Safarni.Application.Features.Billing;
using Safarni.Domain.Enums;

namespace Safarni.API.Controllers;

[ApiController]
[Route("api/pricing")]
public sealed class PricingController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IPaymentGateway _paymentGateway;
    private readonly IBillingRepository _billing;

    public PricingController(ISender sender, IPaymentGateway paymentGateway, IBillingRepository billing)
    {
        _sender = sender;
        _paymentGateway = paymentGateway;
        _billing = billing;
    }

    [AllowAnonymous]
    [HttpGet("plans")]
    public async Task<IActionResult> Plans([FromQuery] PricingCustomerType? customerType, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetPricingPlansQuery(customerType), cancellationToken);
        return result.IsFailure
            ? Failure<IReadOnlyList<PricingPlanResponse>>(result.Error, "Failed to load pricing plans.")
            : Ok(ApiResponse<IReadOnlyList<PricingPlanResponse>>.SuccessResponse(result.Value!, "Pricing plans loaded.", StatusCodes.Status200OK));
    }

    [Authorize]
    [HttpGet("subscription")]
    public Task<IActionResult> Subscription(CancellationToken cancellationToken) => Send<GetMySubscriptionQuery, SubscriptionResponse>(
        new GetMySubscriptionQuery(User.GetRequiredUserId()), "Subscription loaded.", cancellationToken);

    [Authorize]
    [HttpPost("checkout")]
    public Task<IActionResult> Checkout([FromBody] CheckoutRequest request, CancellationToken cancellationToken) => Send<CreateCheckoutSessionCommand, CheckoutResponse>(
        new CreateCheckoutSessionCommand(User.GetRequiredUserId(), request.PlanId), "Checkout session created.", cancellationToken, StatusCodes.Status201Created);

    [Authorize]
    [HttpPost("subscription/cancel")]
    public Task<IActionResult> Cancel(CancellationToken cancellationToken) => Send<CancelMySubscriptionCommand, SubscriptionResponse>(
        new CancelMySubscriptionCommand(User.GetRequiredUserId()), "Subscription cancellation scheduled.", cancellationToken);

    [AllowAnonymous]
    [HttpPost("webhooks/test")]
    public Task<IActionResult> TestWebhook(
        [FromHeader(Name = "X-Payment-Signature")] string signature,
        CancellationToken cancellationToken) => ProcessWebhook(signature, cancellationToken);

    [AllowAnonymous]
    [HttpPost("webhooks/stripe")]
    public Task<IActionResult> StripeWebhook(
        [FromHeader(Name = "Stripe-Signature")] string signature,
        CancellationToken cancellationToken) => ProcessWebhook(signature, cancellationToken);

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
            return Failure<object>(BillingErrors.PaymentWebhookInvalid, "Payment webhook rejected.");
        }

        if (webhook is null)
        {
            return Ok(ApiResponse<object>.SuccessResponse(
                new { ignored = true },
                "Stripe event acknowledged.",
                StatusCodes.Status200OK));
        }

        var result = await _sender.Send(new ProcessPaymentWebhookCommand(webhook), cancellationToken);
        return result.IsFailure
            ? Failure<SubscriptionResponse>(result.Error, "Payment webhook processing failed.")
            : Ok(ApiResponse<SubscriptionResponse>.SuccessResponse(result.Value!, "Payment webhook processed.", StatusCodes.Status200OK));
    }

    [Authorize]
    [HttpPost("test/complete/{sessionId}")]
    public async Task<IActionResult> CompleteTestPayment(string sessionId, [FromQuery] bool success = true, CancellationToken cancellationToken = default)
    {
        var payment = await _billing.GetPaymentBySessionAsync("Test", sessionId, cancellationToken);
        if (payment is null || (payment.Subscription.UserId != User.GetRequiredUserId() && payment.Subscription.TravelAgency?.UserId != User.GetRequiredUserId()))
            return Failure<object>(BillingErrors.PaymentSessionNotFound, "Payment session not found.");

        var webhook = new PaymentWebhookEvent(
            "Test", $"test_event_{Guid.NewGuid():N}", sessionId,
            success ? PaymentStatus.Succeeded : PaymentStatus.Failed,
            success ? null : "The test payment was declined.", DateTime.UtcNow);
        var result = await _sender.Send(new ProcessPaymentWebhookCommand(webhook), cancellationToken);
        return result.IsFailure
            ? Failure<SubscriptionResponse>(result.Error, "Test payment could not be processed.")
            : Ok(ApiResponse<SubscriptionResponse>.SuccessResponse(result.Value!, success ? "Test payment completed." : "Test payment failed.", StatusCodes.Status200OK));
    }

    private async Task<IActionResult> Send<TRequest, TResponse>(TRequest request, string message, CancellationToken cancellationToken, int successStatus = StatusCodes.Status200OK)
        where TRequest : MediatR.IRequest<Result<TResponse>>
    {
        var result = await _sender.Send(request, cancellationToken);
        return result.IsFailure ? Failure<TResponse>(result.Error, message) : StatusCode(successStatus, ApiResponse<TResponse>.SuccessResponse(result.Value!, message, successStatus));
    }

    private ObjectResult Failure<T>(Error error, string message)
    {
        var status = error.Type switch
        {
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };
        return StatusCode(status, ApiResponse<T>.FailureResponse([new ApiError(error.Code, error.Message)], message, status));
    }

    public sealed record CheckoutRequest(Guid PlanId);
}
