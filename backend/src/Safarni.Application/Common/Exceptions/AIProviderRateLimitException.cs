namespace ChatBotApi.Application.Common.Exceptions;

public sealed class AIProviderRateLimitException : Exception
{
    public AIProviderRateLimitException(string message)
        : base(message)
    {
    }
}
