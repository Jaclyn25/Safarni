using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ChatBotApi.Application.Common.DTOs;

namespace ChatBotApi.Application.Common.Interfaces;

public enum ChatIntent
{
    Ambiguous,
    Social,
    OutOfScope,
    RagOnly,
    GooglePlacesOnly,
    Hybrid,
    OutOfDomain
}

public class IntentDetectionResult
{
    public ChatIntent Intent { get; set; }
    public string StandaloneQuery { get; set; } = string.Empty;
    public bool RequiresSemanticClassification { get; set; } = true;
    public bool IsTripPlanningRequest { get; set; }
}

public interface IIntentDetectionService
{
    ChatIntent DetectIntent(string userMessage);
    Task<IntentDetectionResult> DetectIntentAsync(string userMessage, List<ChatMessageDto> history, CancellationToken cancellationToken);
}

