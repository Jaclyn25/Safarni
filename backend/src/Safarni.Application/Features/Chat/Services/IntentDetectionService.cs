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

        if (hasContextDependentIntent || hasDestination)
        {
            return ChatIntent.Ambiguous;
        }

        // Keywords cannot safely distinguish an unknown destination from a person,
        // title, or unrelated topic. Route these messages through semantic scope
        // classification before generating an answer.
        return ChatIntent.Ambiguous;
    }

    public Task<IntentDetectionResult> DetectIntentAsync(
        string userMessage,
        List<ChatMessageDto> history,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(new IntentDetectionResult
        {
            Intent = DetectIntent(userMessage),
            StandaloneQuery = userMessage
        });
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
