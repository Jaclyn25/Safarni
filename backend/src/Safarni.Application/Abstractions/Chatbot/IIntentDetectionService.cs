using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ChatBotApi.Application.Common.DTOs;

namespace ChatBotApi.Application.Common.Interfaces;

public enum ChatIntent
{
    RagOnly,
    GooglePlacesOnly,
    Hybrid,
    OutOfDomain
}

public class IntentDetectionResult
{
    public ChatIntent Intent { get; set; }
    public string StandaloneQuery { get; set; } = string.Empty;
}

public interface IIntentDetectionService
{
    Task<IntentDetectionResult> DetectIntentAsync(string userMessage, List<ChatMessageDto> history, CancellationToken cancellationToken);
}

