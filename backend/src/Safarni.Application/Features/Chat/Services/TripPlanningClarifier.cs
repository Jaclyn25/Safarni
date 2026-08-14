using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ChatBotApi.Application.Common.DTOs;

namespace ChatBotApi.Application.Features.Chat.Services;

/// <summary>
/// Keeps broad trip-planning conversations useful without turning the chat into a form.
/// Only destination and duration can trigger a clarification. Budget, party size and
/// interests are optional refinements; the model proceeds with sensible defaults.
/// </summary>
public static class TripPlanningClarifier
{
    private static readonly Regex DestinationPattern = new(
        @"(?:\b(?:to|visit|visiting|going\s+to|destination(?:\s+is)?)\s+[\p{L}][\p{L}\p{M}\s.'-]{1,45}|\b(?:turkey|china|japan|egypt|france|italy|spain|greece|thailand|maldives|saudi\s+arabia|uae|dubai|istanbul|paris|rome|london|tokyo)\b|\b(?:surprise\s+me|you\s+choose|anywhere)\b|(?:إلى|الى|أزور|ازور|أروح|اروح|وجهتي|الوجهة)\s+[\p{L}\p{M}]{2,}|تركيا|الصين|اليابان|مصر|فرنسا|إيطاليا|ايطاليا|إسبانيا|اسبانيا|اليونان|تايلاند|المالديف|السعودية|الإمارات|الامارات|دبي|إسطنبول|اسطنبول|باريس|روما|لندن|طوكيو|اختار\s+(?:انت|أنت)|أي\s+مكان|اي\s+مكان)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex TimingPattern = new(
        @"(?:\b\d{1,3}\s*(?:days?|weeks?|nights?)\b|\b(?:january|february|march|april|may|june|july|august|september|october|november|december|spring|summer|autumn|fall|winter|next\s+(?:week|month|year)|anytime|flexible|weekend)\b|\b20\d{2}\b|\d{1,3}\s*(?:يوم|أيام|ايام|أسبوع|اسبوع|أسابيع|اسابيع|ليلة|ليالي)|يناير|فبراير|مارس|أبريل|ابريل|مايو|يونيو|يوليو|أغسطس|اغسطس|سبتمبر|أكتوبر|اكتوبر|نوفمبر|ديسمبر|الربيع|الصيف|الخريف|الشتاء|الشهر\s+الجاي|الأسبوع\s+الجاي|الاسبوع\s+الجاي|أي\s+وقت|اي\s+وقت|مرن|نهاية\s+الأسبوع)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static string? BuildClarification(
        bool isTripPlanningRequest,
        string currentMessage,
        IReadOnlyCollection<ChatMessageDto> history)
    {
        if (!isTripPlanningRequest)
        {
            return null;
        }

        var userContext = string.Join(
            "\n",
            history
                .Where(message => string.Equals(message.Role, "user", StringComparison.OrdinalIgnoreCase))
                .Select(message => message.Content)
                .Append(currentMessage));

        var isArabic = ContainsArabic(currentMessage);

        if (!DestinationPattern.IsMatch(userContext))
        {
            return isArabic
                ? "تمام — تحب تسافر فين؟ ولو لسه محتار، قلّي الجو اللي نفسك فيه: بحر وهدوء، أكل وثقافة، ولا مغامرة؟ 😄\n\n---SUGGESTIONS---\nاختار أنت وجهة مناسبة\nبحر واسترخاء\nتاريخ وأكل"
                : "Love it — where would you like to go? If you’re undecided, give me the vibe: beach and calm, food and culture, or an adventure? 😄\n\n---SUGGESTIONS---\nSurprise me with a destination\nBeach and relaxation\nHistory and food";
        }

        if (!TimingPattern.IsMatch(userContext))
        {
            return isArabic
                ? "اختيار جميل! أقدر أبدأ بافتراض رحلة مرنة ومتوازنة لمدة 5 أيام، والميزانية المتوسطة تكون نقطة البداية. السؤال الوحيد اللي هيفرق فعلًا: هتسافر إمتى وعايزها كام يوم؟ ولو مش فارقة، اختار الخطة المرنة وأنا أكمل 😉\n\n---SUGGESTIONS---\nاستخدم خطة مرنة 5 أيام\nرحلة ويك إند"
                : "Great choice! I can start with a flexible, balanced 5-day trip and use mid-range options by default. The one detail that really changes the plan: when are you going and for how long? Or choose the flexible plan and I’ll keep moving 😉\n\n---SUGGESTIONS---\nUse a flexible 5-day plan\nMake it a weekend trip";
        }

        return null;
    }

    private static bool ContainsArabic(string value) =>
        value.Any(character => character is >= '\u0600' and <= '\u06FF');
}
