using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using ChatBotApi.Application.Features.Chat.Commands.CreateConversation;
using ChatBotApi.Application.Features.Chat.Commands.DeleteConversation;
using ChatBotApi.Application.Features.Chat.Commands.RegenerateResponse;
using ChatBotApi.Application.Features.Chat.Commands.SendMessage;
using ChatBotApi.Application.Features.Chat.Queries.GetConversationById;
using ChatBotApi.Application.Features.Chat.Queries.GetConversations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatBotApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
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
            if (string.IsNullOrEmpty(userId))
            {
                return "default-traveler-id";
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
}

public record StartConversationRequest(string? Title);
public record SendMessageRequest(Guid ConversationId, string Message);
public record RegenerateRequest(Guid ConversationId);
public record EditMessageRequest(Guid ConversationId, Guid MessageId, string NewContent);
