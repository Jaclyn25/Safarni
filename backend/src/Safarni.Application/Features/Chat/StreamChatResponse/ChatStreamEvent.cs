using ChatBotApi.Application.Common.DTOs;

namespace ChatBotApi.Application.Features.Chat.StreamChatResponse;

public sealed record ChatStreamEvent(
    string Type,
    string? Token = null,
    ConversationDto? Conversation = null,
    string? Code = null)
{
    public static ChatStreamEvent TokenEvent(string token)
    {
        return new ChatStreamEvent(
            Type: "token",
            Token: token);
    }

    public static ChatStreamEvent CompletedEvent(
        ConversationDto conversation)
    {
        return new ChatStreamEvent(
            Type: "completed",
            Conversation: conversation);
    }
}
