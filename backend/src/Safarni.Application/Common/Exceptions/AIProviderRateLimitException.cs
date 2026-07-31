namespace ChatBotApi.Application.Common.Exceptions;

public sealed class AIProviderRateLimitException : Exception
{
    public AIProviderRateLimitException(
        string message,
        TimeSpan? retryAfter = null)
        : base(message)
    {
        RetryAfter = retryAfter;
    }

    public TimeSpan? RetryAfter { get; }
}
