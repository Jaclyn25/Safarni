using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Safarni.API.Common;

namespace Safarni.API.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException exception)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";

            var response = ApiResponse<object>.FailureResponse(
                exception.Errors.Select(error => new ApiError(error.ErrorCode, error.ErrorMessage)),
                "Validation failed.",
                StatusCodes.Status400BadRequest);

            await context.Response.WriteAsJsonAsync(response);
        }
        catch (Exception)
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var response = ApiResponse<object>.FailureResponse(
                [new ApiError("Server.Error", "An unexpected error occurred.")],
                "An unexpected error occurred.",
                StatusCodes.Status500InternalServerError);

            await context.Response.WriteAsJsonAsync(response);
        }
    }
}