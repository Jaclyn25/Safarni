using System;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ChatBotApi.Application.Features.Chat.Commands.CreateConversation;
using ChatBotApi.Application.Features.Chat.Commands.DeleteConversation;
using ChatBotApi.Application.Features.Chat.Commands.RegenerateResponse;
using ChatBotApi.Application.Features.Chat.Commands.SendMessage;
using ChatBotApi.Application.Features.Chat.Queries.GetConversationById;
using ChatBotApi.Application.Features.Chat.Queries.GetConversations;
using ChatBotApi.Application.Features.Chat.StreamChatResponse;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Safarni.API.Contracts.Chat;

namespace ChatBotApi.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private static readonly JsonSerializerOptions
        StreamJsonOptions =
            new(JsonSerializerDefaults.Web);

    private readonly IMediator _mediator;

    public ChatController(IMediator mediator)
    {
        _mediator = mediator;
    }

    private string CurrentUserId
    {
        get
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new UnauthorizedAccessException("لا توجد هوية مستخدم صالحة لهذه الجلسة.");
            }
            return userId;
        }
    }

    [HttpPost("conversations")]
    public async Task<IActionResult> StartConversation([FromBody] StartConversationRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateConversationCommand(CurrentUserId, request.Title);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations(CancellationToken cancellationToken)
    {
        var query = new GetConversationsQuery(CurrentUserId);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("conversations/{id:guid}")]
    public async Task<IActionResult> GetConversationById(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetConversationByIdQuery(id, CurrentUserId);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request, CancellationToken cancellationToken)
    {
        var command = new SendMessageCommand(request.ConversationId, CurrentUserId, request.Message);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("stream")]
    [Produces("text/event-stream")]
    public async Task StreamResponse(
        [FromBody] StreamChatRequest request,
        CancellationToken cancellationToken)
    {
        var command = new StreamChatResponseCommand(
            request.ConversationId,
            CurrentUserId,
            request.Message);

        var stream = _mediator.CreateStream(
            command,
            cancellationToken);

        await using var enumerator =
            stream.GetAsyncEnumerator(cancellationToken);

        ChatStreamEvent firstEvent;

        try
        {
            if (!await enumerator.MoveNextAsync())
            {
                await WriteJsonErrorAsync(
                    StatusCodes.Status502BadGateway,
                    "The AI provider returned an empty response.",
                    cancellationToken);

                return;
            }

            firstEvent = enumerator.Current;
        }
        catch (ValidationException exception)
        {
            var message = exception.Errors
                .FirstOrDefault()
                ?.ErrorMessage ??
                "The chat request is invalid.";

            await WriteJsonErrorAsync(
                StatusCodes.Status400BadRequest,
                message,
                cancellationToken);

            return;
        }
        catch (KeyNotFoundException exception)
        {
            await WriteJsonErrorAsync(
                StatusCodes.Status404NotFound,
                exception.Message,
                cancellationToken);

            return;
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception)
        {
            await WriteJsonErrorAsync(
                StatusCodes.Status500InternalServerError,
                "The assistant could not generate a response.",
                cancellationToken);

            return;
        }

        HttpContext.Features
            .Get<IHttpResponseBodyFeature>()
            ?.DisableBuffering();

        Response.StatusCode = StatusCodes.Status200OK;
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";
        Response.Headers["X-Accel-Buffering"] = "no";

        await WriteStreamEventAsync(
            firstEvent,
            cancellationToken);

        try
        {
            while (await enumerator.MoveNextAsync())
            {
                await WriteStreamEventAsync(
                    enumerator.Current,
                    cancellationToken);
            }
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            // The user stopped the response. Partial assistant text is not saved.
        }
        catch (Exception)
        {
            await WriteStreamEventAsync(
                new ChatStreamEvent(
                    Type: "error",
                    Token:
                        "The assistant could not complete the response."),
                CancellationToken.None);
        }
    }

    [HttpPost("regenerate")]
    public async Task<IActionResult> RegenerateResponse([FromBody] RegenerateRequest request, CancellationToken cancellationToken)
    {
        var command = new RegenerateResponseCommand(request.ConversationId, CurrentUserId);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("edit")]
    public async Task<IActionResult> EditMessage([FromBody] EditMessageRequest request, CancellationToken cancellationToken)
    {
        var command = new ChatBotApi.Application.Features.Chat.Commands.EditMessage.EditMessageCommand(request.ConversationId, CurrentUserId, request.MessageId, request.NewContent);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("conversations/{id:guid}")]
    public async Task<IActionResult> DeleteConversation(Guid id, CancellationToken cancellationToken)
    {
        var command = new DeleteConversationCommand(id, CurrentUserId);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(new { success = result });
    }

    private async Task WriteStreamEventAsync(
        ChatStreamEvent streamEvent,
        CancellationToken cancellationToken)
    {
        var data = JsonSerializer.Serialize(
            streamEvent,
            StreamJsonOptions);

        await Response.WriteAsync(
            $"event: {streamEvent.Type}\n" +
            $"data: {data}\n\n",
            cancellationToken);

        await Response.Body.FlushAsync(
            cancellationToken);
    }

    private async Task WriteJsonErrorAsync(
        int statusCode,
        string message,
        CancellationToken cancellationToken)
    {
        Response.StatusCode = statusCode;
        Response.ContentType = "application/json";

        await Response.WriteAsJsonAsync(
            new
            {
                message
            },
            cancellationToken);
    }
}

public record StartConversationRequest(string? Title);
public record SendMessageRequest(Guid ConversationId, string Message);
public record RegenerateRequest(Guid ConversationId);
public record EditMessageRequest(Guid ConversationId, Guid MessageId, string NewContent);
