namespace ChatBotApi.Application.Common.Interfaces;

public enum ChatIntent
{
    Social,
    OutOfScope,
    RagOnly,
    GooglePlacesOnly,
    Hybrid
}

public interface IIntentDetectionService
{
    ChatIntent DetectIntent(string userMessage);
}
