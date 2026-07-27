namespace ChatBotApi.Application.Common.Interfaces;

public enum ChatIntent
{
    RagOnly,
    GooglePlacesOnly,
    Hybrid
}

public interface IIntentDetectionService
{
    ChatIntent DetectIntent(string userMessage);
}
