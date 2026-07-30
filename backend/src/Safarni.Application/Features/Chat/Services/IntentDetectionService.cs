using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ChatBotApi.Application.Common.DTOs;
using ChatBotApi.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace ChatBotApi.Application.Features.Chat.Services;

public class IntentDetectionService : IIntentDetectionService
{
    private readonly IAIProvider _aiProvider;
    private readonly ILogger<IntentDetectionService> _logger;

    private static readonly string[] PlaceKeywords = { 
        "فندق", "فنادق", "مطعم", "مطاعم", "كافيه", "مقهى", "اماكن", "أماكن", "سياحية", "سياحيه", 
        "مزار", "مزارات", "نشاط", "انشطة", "أنشطة", "حديقة", "حديقه", "مول", "سوق", "اسواق", "أسواق", "شاطئ", "شواطئ" 
    };

    private static readonly string[] NonTravelKeywords = {
        "برمجة", "برمجه", "كود", "رياضيات", "طب", "فيزياء", "سيارات", "جوال", "لابتوب", "شاشة", "كمبيوتر", "سياسة", "اقتصاد"
    };

    public IntentDetectionService(IAIProvider aiProvider, ILogger<IntentDetectionService> logger)
    {
        _aiProvider = aiProvider;
        _logger = logger;
    }

    public async Task<IntentDetectionResult> DetectIntentAsync(string userMessage, List<ChatMessageDto> history, CancellationToken cancellationToken)
    {
        var historyContext = string.Join("\n", history.TakeLast(6).Select(h => $"{h.Role}: {h.Content}"));
        var systemPrompt = @"أنت محلل نصوص سياحي خبير. مهمتك قراءة رسالة المستخدم وتاريخ المحادثة واستخراج شيئين:
1. 'Intent': تصنيف نية المستخدم إلى إحدى الفئات التالية:
   - 'OutOfDomain': إذا كان السؤال خارج نطاق السفر والسياحة تماماً (مثل البرمجة، الطب، الرياضيات، التكنولوجيا، إلخ).
   - 'Hybrid': إذا كان يبحث عن أماكن محددة على الخريطة (فنادق، مطاعم، مزارات، كافيهات) أو تفاصيلها (عناوين، تقييمات، أسعار).
   - 'RagOnly': للأسئلة السياحية العامة، النصائح، الطقس، الميزانية، التأشيرات (الفيزا)، خطط الرحلات (Itineraries)، وتفاصيل الوجهات، أو الأسئلة العامة.

2. 'StandaloneQuery': قم بإعادة صياغة سؤال المستخدم الحالي ليصبح سؤالاً مستقلاً بذاته (Standalone) يحمل كل السياق من المحادثة السابقة.
   مثال:
   - المستخدم: 'أرشح لي فندقاً في باريس.'
   - المستخدم: 'يكون قريب من المترو.' -> StandaloneQuery: 'فنادق في باريس قريبة من المترو.'
   - المستخدم: 'وهل يوجد إفطار؟' -> StandaloneQuery: 'فنادق في باريس قريبة من المترو مع إفطار.'

يجب أن تعيد الإجابة بتنسيق JSON فقط وبدون أي نصوص إضافية، هكذا:
{
    ""intent"": ""Hybrid"",
    ""standaloneQuery"": ""...""
}
";

        var userPrompt = $"تاريخ المحادثة:\n{historyContext}\n\nالرسالة الحالية:\n{userMessage}";

        try
        {
            var response = await _aiProvider.GetChatCompletionAsync(systemPrompt, new List<ChatMessageDto>(), userPrompt, cancellationToken);
            
            // Clean markdown if present
            var jsonStr = response.Trim();
            if (jsonStr.StartsWith("```json"))
            {
                jsonStr = jsonStr.Substring(7);
                if (jsonStr.EndsWith("```"))
                {
                    jsonStr = jsonStr.Substring(0, jsonStr.Length - 3);
                }
            }
            jsonStr = jsonStr.Trim();

            var resultDocument = JsonDocument.Parse(jsonStr);
            var root = resultDocument.RootElement;
            
            var intentStr = root.GetProperty("intent").GetString();
            var standaloneQuery = root.GetProperty("standaloneQuery").GetString();

            if (!Enum.TryParse<ChatIntent>(intentStr, true, out var intent))
            {
                intent = FallbackHeuristicDetection(standaloneQuery ?? userMessage);
            }

            return new IntentDetectionResult
            {
                Intent = intent,
                StandaloneQuery = string.IsNullOrWhiteSpace(standaloneQuery) ? userMessage : standaloneQuery
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to use LLM for intent detection. Falling back to heuristic.");
            var fallbackIntent = FallbackHeuristicDetection(userMessage);
            return new IntentDetectionResult
            {
                Intent = fallbackIntent,
                StandaloneQuery = userMessage
            };
        }
    }

    private ChatIntent FallbackHeuristicDetection(string userMessage)
    {
        var lowerMsg = userMessage.ToLower();

        // 1. Check for OutOfDomain explicitly
        if (NonTravelKeywords.Any(kw => lowerMsg.Contains(kw)))
        {
            return ChatIntent.OutOfDomain;
        }

        // 2. Check for Places intent
        if (PlaceKeywords.Any(kw => lowerMsg.Contains(kw)))
        {
            return ChatIntent.Hybrid; // We use hybrid to check RAG first, then Google Places
        }

        // 3. Check for specific commands or RAG-heavy topics
        if (lowerMsg.Contains("فيزا") || lowerMsg.Contains("تاشيرة") || lowerMsg.Contains("تأشيرة") ||
            lowerMsg.Contains("سعر") || lowerMsg.Contains("تكلفة") || lowerMsg.Contains("تكلفه") ||
            lowerMsg.Contains("تذكرة") || lowerMsg.Contains("طيران") || lowerMsg.Contains("طقس") ||
            lowerMsg.Contains("جو") || lowerMsg.Contains("امان") || lowerMsg.Contains("أمان"))
        {
            return ChatIntent.RagOnly;
        }

        // 4. Default to RagOnly for general conversations (greetings, general advice, etc.)
        return ChatIntent.RagOnly;
    }
}
