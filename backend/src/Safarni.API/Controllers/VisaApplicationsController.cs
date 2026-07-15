using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Safarni.API.Common;
using Safarni.Application.Common.Errors;
using Safarni.Application.Features.VisaApplications;
using Safarni.Application.Features.VisaApplications.CreateVisaApplication;
using Safarni.Application.Features.VisaApplications.GetVisaApplication;
using Safarni.Application.Features.VisaApplications.ListVisaApplications;
using Safarni.Application.Features.VisaApplications.SelectVisaType;

namespace Safarni.API.Controllers;

[ApiController]
[Authorize]
[Route("api/visa-applications")]
public sealed class VisaApplicationsController : ControllerBase
{
    private readonly ISender _sender;

    public VisaApplicationsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<VisaApplicationResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<VisaApplicationResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<VisaApplicationResponse>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create(
        [FromBody] CreateVisaApplicationCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return Failure<VisaApplicationResponse>(result.Error, "Visa application creation failed.");
        }

        var response = ApiResponse<VisaApplicationResponse>.SuccessResponse(
            result.Value!,
            "Visa application created successfully.",
            StatusCodes.Status201Created);

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, response);
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<VisaApplicationResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<VisaApplicationResponse>>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListVisaApplicationsQuery(), cancellationToken);

        if (result.IsFailure)
        {
            return Failure<IReadOnlyCollection<VisaApplicationResponse>>(result.Error, "Visa application lookup failed.");
        }

        return Ok(ApiResponse<IReadOnlyCollection<VisaApplicationResponse>>.SuccessResponse(
            result.Value!,
            "Visa applications retrieved successfully.",
            StatusCodes.Status200OK));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<VisaApplicationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<VisaApplicationResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<VisaApplicationResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetVisaApplicationQuery(id), cancellationToken);

        if (result.IsFailure)
        {
            return Failure<VisaApplicationResponse>(result.Error, "Visa application lookup failed.");
        }

        return Ok(ApiResponse<VisaApplicationResponse>.SuccessResponse(
            result.Value!,
            "Visa application retrieved successfully.",
            StatusCodes.Status200OK));
    }

    [HttpPut("{id:guid}/visa-type")]
    [ProducesResponseType(typeof(ApiResponse<VisaApplicationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<VisaApplicationResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<VisaApplicationResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<VisaApplicationResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SelectVisaType(
        Guid id,
        [FromBody] SelectVisaTypeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new SelectVisaTypeCommand(id, request.VisaType), cancellationToken);

        if (result.IsFailure)
        {
            return Failure<VisaApplicationResponse>(result.Error, "Visa type selection failed.");
        }

        return Ok(ApiResponse<VisaApplicationResponse>.SuccessResponse(
            result.Value!,
            "Visa type selected successfully.",
            StatusCodes.Status200OK));
    }

    private ObjectResult Failure<T>(Error error, string message)
    {
        var statusCode = MapStatusCode(error.Type);
        var response = ApiResponse<T>.FailureResponse(
            [new ApiError(error.Code, error.Message)],
            message,
            statusCode);

        return StatusCode(statusCode, response);
    }

    private static int MapStatusCode(ErrorType errorType)
    {
        return errorType switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError
        };
    }
}

public sealed record SelectVisaTypeRequest(string VisaType);
