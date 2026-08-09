using FluentValidation;
using ChatBotApi.Application.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        catch (DbUpdateConcurrencyException exception)
        {
            var entityNames = string.Join(
                ", ",
                exception.Entries
                    .Select(entry => entry.Metadata.ClrType.Name)
                    .Distinct());
            Console.WriteLine("========== DATABASE CONCURRENCY CONFLICT ==========");
            Console.WriteLine(entityNames);
            Console.WriteLine("====================================================");
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            context.Response.ContentType = "application/json";

            var response = ApiResponse<object>.FailureResponse(
                [new ApiError(
                    "Data.ConcurrencyConflict",
                    $"The record was changed by another request. Affected entity: {entityNames}. Reload and try again.")],
                "The saved data changed while this request was running.",
                StatusCodes.Status409Conflict);

            await context.Response.WriteAsJsonAsync(response);
        }
        catch (AIProviderRateLimitException)
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.ContentType = "application/json";

            var response = ApiResponse<object>.FailureResponse(
                [new ApiError(
                    "Chat.SessionLimitReached",
                    "وصلت هذه المحادثة إلى الحد المتاح حاليًا. ابدأ شات جديد للمتابعة.")],
                "Start a new chat to continue.",
                StatusCodes.Status429TooManyRequests);

            await context.Response.WriteAsJsonAsync(response);
        }
        catch (Exception ex)
        {
            Console.WriteLine("========== UNHANDLED EXCEPTION ==========");
            Console.WriteLine(ex.ToString());
            Console.WriteLine("=========================================");

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
