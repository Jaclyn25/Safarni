using Microsoft.AspNetCore.Mvc;
using Safarni.API.Common;
using Safarni.Application.Common.Errors;

namespace Safarni.API.Controllers.Admin;

[ApiController]
public abstract class AdminControllerBase : ControllerBase
{
    protected ObjectResult Failure<T>(Error error, string message)
    {
        var statusCode = MapStatusCode(error.Type);
        var response = ApiResponse<T>.FailureResponse(
            [new ApiError(error.Code, error.Message)],
            message,
            statusCode);

        return StatusCode(statusCode, response);
    }

    private static int MapStatusCode(ErrorType errorType) => errorType switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        _ => StatusCodes.Status500InternalServerError
    };
}
