using ChatBotApi.Application.Common.DTOs;

namespace ChatBotApi.Application.Features.Chat.StreamChatResponse;

public sealed record ChatStreamEvent(
    string Type,
    string? Token = null,
    ConversationDto? Conversation = null,
    string? Code = null,
    string? Status = null,
    IReadOnlyList<ChatSource>? Sources = null,
    IReadOnlyList<ChatPlaceCard>? Places = null)
{
    public static ChatStreamEvent TokenEvent(string token)
    {
        return new ChatStreamEvent(
            Type: "token",
            Token: token);
    }

    public static ChatStreamEvent StatusEvent(string status)
    {
        return new ChatStreamEvent(
            Type: "status",
            Status: status);
    }

    public static ChatStreamEvent ResourcesEvent(
        IReadOnlyList<ChatSource> sources,
        IReadOnlyList<ChatPlaceCard> places)
    {
        return new ChatStreamEvent(
            Type: "resources",
            Sources: sources,
            Places: places);
    }

    public static ChatStreamEvent CompletedEvent(
        ConversationDto conversation)
    {
        return new ChatStreamEvent(
            Type: "completed",
            Conversation: conversation);
    }
}

public sealed record ChatSource(
    string Title,
    string Url,
    string Domain,
    string Kind);

public sealed record ChatPlaceCard(
    string Name,
    string Address,
    double? Rating,
    int? ReviewCount,
    string Url,
    string? Description);
