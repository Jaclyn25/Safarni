using System;
using System.Collections.Generic;
using System.Linq;
using ChatBotApi.Application.Common.Interfaces;

namespace ChatBotApi.Application.Features.Chat.Services;

public class IntentDetectionService : IIntentDetectionService
{
    private static readonly string[] PlacesKeywords = new[]
    {
        "restaurant", "restaurants", "hotel", "hotels", "cafe", "cafes", "coffee", "coffee shop", "coffee shops",
        "attraction", "attractions", "museum", "museums", "park", "parks", "shopping", "mall", "malls",
        "airport", "airports", "hospital", "hospitals", "pharmacy", "pharmacies", "tourist place", "tourist places",
        "place near me", "places near me", "places in", "hotel in", "restaurant in", "where to eat", "where to stay",
        "lodging", "resort", "resorts",
        "مطعم", "مطاعم", "فندق", "فنادق", "كافيه", "كافيهات", "مقهى", "مقاهي", "قهوة",
        "متحف", "متاحف", "حديقة", "حدائق", "منتزه", "منتزهات", "مول", "مولات", "تسوق",
        "مطار", "مطارات", "مستشفى", "مستشفيات", "صيدلية", "صيدليات", "أماكن سياحية", "اماكن سياحية",
        "أماكن", "اماكن", "مكان", "قريبة مني", "في روما", "في القاهرة", "في دبي", "في باريس",
        "اقترح لي فنادق", "افضل المطاعم", "أفضل المطاعم", "أفضل الفنادق", "افضل الفنادق", "أين أقيم", "وين أسكن"
    };

    private static readonly string[] RagKeywords = new[]
    {
        "best time to visit", "travel tips", "transportation", "itinerary", "travel guide", "local customs",
        "safety", "weather overview", "weather", "country information", "visa", "requirements", "culture",
        "history", "how to travel", "advice", "budget", "currency", "best month", "climate", "days plan",
        "أفضل وقت لزيارة", "افضل وقت لزيارة", "نصائح سفر", "نصائح للسفر", "مواصلات", "جدول سياحي", "برنامج سياحي",
        "دليل سفر", "عادات وتقاليد", "أمان", "امان", "طقس", "مناخ", "معلومات عن", "تأشيرة", "فيزا",
        "كم يوم", "خطة سفر", "تكلفة", "عملة", "تاريخ", "ثقافة", "كيف اتنقل", "المواصلات"
    };

    public ChatIntent DetectIntent(string userMessage)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
        {
            return ChatIntent.RagOnly;
        }

        var text = userMessage.ToLower();

        bool hasPlacesIntent = PlacesKeywords.Any(k => text.Contains(k.ToLower()));
        bool hasRagIntent = RagKeywords.Any(k => text.Contains(k.ToLower()));

        // Explicit check for day plans / combined requests like "Rome for 5 days recommend hotels and itinerary"
        if ((text.Contains("days") || text.Contains("أيام") || text.Contains("ايام") || text.Contains("يوم") || text.Contains("plan") || text.Contains("itinerary"))
            && hasPlacesIntent)
        {
            hasRagIntent = true;
        }

        if (hasPlacesIntent && hasRagIntent)
        {
            return ChatIntent.Hybrid;
        }

        if (hasPlacesIntent)
        {
            return ChatIntent.GooglePlacesOnly;
        }

        return ChatIntent.RagOnly;
    }
}
