using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Safarni.API.Common;
using Safarni.API.Extensions;
using Safarni.Application.Common.Errors;
using Safarni.Application.Features.Agencies.Payout;

namespace Safarni.API.Controllers;

[ApiController]
[Authorize(Policy = AccountPolicies.TravelAgencyOnly)]
[Route("api/agency/payout-profile")]
public sealed class AgencyPayoutController : ControllerBase
{
    private readonly ISender _sender;

    public AgencyPayoutController(ISender sender) => _sender = sender;

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetMyAgencyPayoutProfileQuery(User.GetRequiredUserId()), cancellationToken);
        return result.IsFailure
            ? Failure<AgencyPayoutProfileResponse>(result.Error, "Payout profile is not configured yet.")
            : Ok(ApiResponse<AgencyPayoutProfileResponse>.SuccessResponse(result.Value!, "Payout profile loaded.", StatusCodes.Status200OK));
    }

    [HttpPut]
    public async Task<IActionResult> Save([FromBody] SavePayoutProfileRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new UpsertMyAgencyPayoutProfileCommand(
            User.GetRequiredUserId(), request.Method, request.AccountHolderName, request.Country,
            request.BankName, request.Iban, request.AccountNumber, request.SwiftCode,
            request.WalletProvider, request.WalletNumber), cancellationToken);
        return result.IsFailure
            ? Failure<AgencyPayoutProfileResponse>(result.Error, "Payout profile could not be saved.")
            : Ok(ApiResponse<AgencyPayoutProfileResponse>.SuccessResponse(result.Value!, "Payout profile saved for review.", StatusCodes.Status200OK));
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

    public sealed record SavePayoutProfileRequest(
        Domain.Enums.AgencyPayoutMethod Method,
        string AccountHolderName,
        string Country,
        string? BankName,
        string? Iban,
        string? AccountNumber,
        string? SwiftCode,
        string? WalletProvider,
        string? WalletNumber);
}
