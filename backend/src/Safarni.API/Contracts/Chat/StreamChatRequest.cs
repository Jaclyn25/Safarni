namespace Safarni.API.Contracts.Chat;

public sealed record StreamChatRequest(
    Guid ConversationId,
    string Message);
