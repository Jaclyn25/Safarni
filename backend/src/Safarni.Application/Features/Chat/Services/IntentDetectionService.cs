using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ChatBotApi.Application.Common.DTOs;
using ChatBotApi.Application.Common.Interfaces;

namespace ChatBotApi.Application.Features.Chat.Services;

public class IntentDetectionService : IIntentDetectionService
{
    private readonly IAIProvider _aiProvider;

    public IntentDetectionService(IAIProvider aiProvider)
    {
        _aiProvider = aiProvider;
    }

    private static readonly string[] PlacesKeywords =
    {
        "restaurant", "restaurants", "hotel", "hotels", "cafe", "cafes", "coffee shop",
        "attraction", "attractions", "museum", "museums", "park", "parks", "shopping", "mall",
        "airport", "hospital", "pharmacy", "tourist place", "places near me", "where to eat",
        "where to stay", "lodging", "resort", "near me",
        "مطعم", "مطاعم", "فندق", "فنادق", "كافيه", "مقهى", "متحف", "متاحف",
        "حديقة", "مول", "تسوق", "مطار", "مستشفى", "صيدلية", "اماكن", "أماكن",
        "مكان", "فين اخرج", "اين اقيم"
    };

    private static readonly string[] StrongTravelKeywords =
    {
        "travel", "trip", "tourism", "tourist", "destination", "visit", "visa", "flight",
        "itinerary", "weather", "currency", "budget", "transportation", "customs",
        "سفر", "رحلة", "رحلات", "سياحة", "سياحي", "سياحية", "زيارة", "ازور",
        "وجهة", "وجهات", "فيزا", "تأشيرة", "طيران", "مواصلات", "جدول", "خطة",
        "طقس", "عملة", "تكلفة", "ميزانية"
    };

    private static readonly string[] PlanningKeywords =
    {
        "itinerary", "plan a trip", "plan my trip", "trip plan", "travel plan",
        "day trip", "days in", "build a trip", "create a trip", "suggest a trip",
        "recommend a trip", "what to do", "things to do", "i want to travel", "i want to go",
        "help me travel", "plan a vacation", "plan a holiday",
        "جدول رحلة", "خطة رحلة", "خطط لي", "برنامج سياحي", "اقترح رحلة", "اعمل لي رحلة",
        "عاوز اسافر", "عايز اسافر", "عاوز أروح", "عايز أروح", "عاوز اروح", "عايز اروح",
        "أريد السفر", "اريد السفر", "أريد الذهاب", "اريد الذهاب", "نفسي اسافر", "نفسي أروح", "نفسي اروح"
    };

    private static readonly string[] ContextDependentTravelKeywords =
    {
        "guide", "culture", "history", "capital", "country", "city", "safety", "climate",
        "event", "festival", "music", "film", "sports",
        "برنامج", "دليل", "مناخ", "ثقافة", "تاريخ", "عادات", "عاصمة", "دولة",
        "مدينة", "امان", "أمان", "فعالية", "مهرجان", "موسيقى", "فيلم", "رياضة"
    };

    private static readonly string[] DestinationKeywords =
    {
        "egypt", "cairo", "giza", "luxor", "aswan", "alexandria", "usa", "america",
        "united states", "new york", "los angeles", "california", "turkey", "istanbul",
        "ankara", "uae", "dubai", "abu dhabi", "france", "paris", "italy", "rome",
        "spain", "barcelona", "london", "uk", "greece", "japan", "thailand", "maldives",
        "مصر", "القاهرة", "الجيزة", "اسوان", "الاقصر", "اسكندرية", "امريكا",
        "أمريكا", "الولايات", "نيويورك", "تركيا", "اسطنبول", "انقرة", "الامارات",
        "دبي", "ابوظبي", "فرنسا", "باريس", "ايطاليا", "روما", "اسبانيا",
        "برشلونة", "لندن", "اليونان", "اليابان", "تايلاند", "المالديف"
    };

    private static readonly string[] SocialKeywords =
    {
        "hi", "hello", "hey", "good morning", "good evening", "how are you", "how are things",
        "thank you", "thanks", "who are you", "what are you", "bye", "goodbye",
        "you are stupid", "bad answer", "wrong answer", "not helpful",
        "اهلا", "أهلا", "مرحبا", "هاي", "هلا", "ازيك", "السلام عليكم",
        "صباح الخير", "مساء الخير", "عامل اي", "عامل ايه", "اخبارك", "كيفك",
        "شلونك", "شكرا", "تسلم", "ميرسي", "انت مين", "مين انت", "باي",
        "مع السلامه", "انت غبي", "غبي", "ردك وحش", "مش فاهم", "مش فاهمك",
        "كلامك غلط", "انت مبتفهمش"
    };

    private static readonly string[] ExplicitOutOfScopeKeywords =
    {
        "programming", "software", "source code", "javascript", "typescript", "python", "c#",
        "java", "sql query", "algorithm", "debug", "compiler", "homework", "solve equation",
        "algebra", "calculus", "physics", "chemistry", "medical diagnosis", "prescription",
        "stock trading", "crypto trading", "football", "match score", "sports score",
        "political news", "breaking news",
        "برمجة", "كود", "شفرة", "جافاسكريبت", "بايثون", "خوارزمية", "واجب",
        "معادلة", "جبر", "تفاضل", "فيزياء", "كيمياء", "تشخيص طبي", "روشتة",
        "بورصة", "تداول", "مباراة", "كرة القدم", "نتيجة المباراة", "نتيجة مباراة",
        "اخبار سياسية", "أخبار سياسية"
    };

    private static readonly Regex StandaloneMathExpression = new(
        @"^\s*(?:what is|calculate|solve|احسب|حل)?\s*\d+(?:\s*[-+*/=×÷]\s*\d+)+\s*[?؟]?\s*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public ChatIntent DetectIntent(string userMessage)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
        {
            return ChatIntent.OutOfScope;
        }

        var text = Normalize(userMessage);

        var hasPlacesIntent = ContainsAny(text, PlacesKeywords);
        var hasTravelIntent = ContainsAny(text, StrongTravelKeywords);
        var hasPlanningIntent = ContainsAny(text, PlanningKeywords);
        var hasContextDependentIntent = ContainsAny(text, ContextDependentTravelKeywords);
        var hasDestination = ContainsAny(text, DestinationKeywords);
        var hasSocialIntent = ContainsAny(text, SocialKeywords);
        var isClearlyOutOfScope = ContainsAny(text, ExplicitOutOfScopeKeywords)
            || StandaloneMathExpression.IsMatch(text);

        if (isClearlyOutOfScope &&
            (hasPlacesIntent || hasTravelIntent || hasContextDependentIntent || hasDestination))
        {
            return ChatIntent.Ambiguous;
        }

        if (isClearlyOutOfScope)
        {
            return ChatIntent.OutOfScope;
        }

        if (hasSocialIntent &&
            (hasPlacesIntent || hasTravelIntent || hasContextDependentIntent || hasDestination))
        {
            return ChatIntent.Ambiguous;
        }

        if (hasPlanningIntent && (hasTravelIntent || hasDestination))
        {
            return ChatIntent.Hybrid;
        }

        if (hasPlacesIntent && (hasTravelIntent || hasDestination))
        {
            return ChatIntent.Hybrid;
        }

        if (hasPlacesIntent)
        {
            return ChatIntent.GooglePlacesOnly;
        }

        if (hasTravelIntent)
        {
            return ChatIntent.RagOnly;
        }

        if (hasSocialIntent)
        {
            return ChatIntent.Social;
        }

        if (hasContextDependentIntent && hasDestination)
        {
            return ChatIntent.RagOnly;
        }

        if (hasContextDependentIntent || hasDestination)
        {
            return ChatIntent.Ambiguous;
        }

        // Keywords cannot safely distinguish an unknown destination from a person,
        // title, or unrelated topic. Route these messages through semantic scope
        // classification before generating an answer.
        return ChatIntent.Ambiguous;
    }

    public async Task<IntentDetectionResult> DetectIntentAsync(
        string userMessage,
        List<ChatMessageDto> history,
        CancellationToken cancellationToken)
    {
        var intent = DetectIntent(userMessage);
        var requiresSemanticClassification = intent is ChatIntent.Ambiguous or ChatIntent.Social;
        var isTripPlanningRequest = IsTripPlanningMessage(userMessage) ||
            history?.Any(message =>
                string.Equals(message.Role, "user", System.StringComparison.OrdinalIgnoreCase) &&
                IsTripPlanningMessage(message.Content)) == true;

        if (isTripPlanningRequest && intent == ChatIntent.Ambiguous)
        {
            intent = ChatIntent.Hybrid;
        }
        var standaloneQuery = userMessage;

        // Only rewrite if intent requires contextual searching
        if (intent is ChatIntent.RagOnly or ChatIntent.GooglePlacesOnly or ChatIntent.Hybrid or ChatIntent.Ambiguous)
        {
            if (history != null && history.Count > 0)
            {
                var prompt = $@"
You are a helpful travel query rewriter. 
Given the user's latest message and the chat history, rewrite the user's latest message into a standalone search query that contains all necessary context (like the city, hotel name, or subject) so it can be used to search Google Places or a travel database independently.
If the latest message is already self-contained, just return the latest message.
DO NOT answer the question. ONLY return the rewritten query. Keep it very concise.
Latest message: {userMessage}
";
                try
                {
                    var options = new ChatBotApi.Application.Common.Models.AICompletionOptions(0.0f, 100, 0.1f);
                    standaloneQuery = await _aiProvider.GetChatCompletionAsync(prompt, history, "Rewrite this query:", options, cancellationToken);
                    if (string.IsNullOrWhiteSpace(standaloneQuery))
                    {
                        standaloneQuery = userMessage;
                    }
                }
                catch
                {
                    // Fallback to original message if rewriting fails
                    standaloneQuery = userMessage;
                }
            }
        }

        return new IntentDetectionResult
        {
            Intent = intent,
            StandaloneQuery = standaloneQuery.Trim(),
            RequiresSemanticClassification = requiresSemanticClassification,
            IsTripPlanningRequest = isTripPlanningRequest
        };
    }

    private static bool IsTripPlanningMessage(string message)
    {
        return !string.IsNullOrWhiteSpace(message) &&
               ContainsAny(Normalize(message), PlanningKeywords);
    }

    private static bool ContainsAny(string text, string[] keywords)
    {
        return keywords.Any(keyword =>
        {
            var normalizedKeyword = Normalize(keyword);
            var pattern = $@"(?<![\p{{L}}\p{{N}}]){Regex.Escape(normalizedKeyword)}(?![\p{{L}}\p{{N}}])";
            return Regex.IsMatch(text, pattern, RegexOptions.CultureInvariant);
        });
    }

    private static string Normalize(string text)
    {
        var normalized = text.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            builder.Append(c switch
            {
                'أ' or 'إ' or 'آ' => 'ا',
                'ى' => 'ي',
                'ة' => 'ه',
                'ؤ' => 'و',
                'ئ' => 'ي',
                _ => c
            });
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
