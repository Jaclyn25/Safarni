using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Safarni.API.Common;
using Safarni.Application.Features.Ai.PlanTrip;

namespace Safarni.API.Controllers;

[ApiController]
[Authorize]
[Route("api/ai/plan-trip")]
public sealed class AiTripPlanController : ControllerBase
{
    private readonly ISender _sender;

    public AiTripPlanController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    [ProducesResponseType(
        typeof(ApiResponse<AiTripPlanResponse>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> PlanTripAi(
        [FromBody] AiTripPlanRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new PlanTripAiCommand(request.Prompt), cancellationToken);

        if (result.IsFailure)
        {
            return Failure<AiTripPlanResponse>(
                result.Error,
                "Failed to plan trip using AI.");
        }

        return Ok(ApiResponse<AiTripPlanResponse>.SuccessResponse(
            result.Value!,
            "AI Trip Plan and related public trips loaded successfully.",
            StatusCodes.Status200OK));
    }

    private ObjectResult Failure<T>(Safarni.Application.Common.Errors.Error error, string message)
    {
        return BadRequest(ApiResponse<T>.FailureResponse(
            new[] { new ApiError(error.Code, error.Message) },
            message,
            StatusCodes.Status400BadRequest));
    }
}
