using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChatBotApi.Application.Common.Interfaces;
using ChatBotApi.Application.Common.Models.GooglePlaces;
using ChatBotApi.Domain.Entities;

namespace ChatBotApi.Application.Features.Chat.Services;

public class PromptBuilder : IPromptBuilder
{
    public string BuildSystemPrompt(List<DocumentChunk> contextChunks, string userMessage)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are Safarni AI, a specialized intelligent travel and tourism assistant.");
        sb.AppendLine("CRITICAL RULE 1: Your primary goal is to help users with travel, tourism, and destination information.");
        sb.AppendLine("CRITICAL RULE 2: You MUST use the provided Chat History to understand conversational follow-up questions (e.g., 'Are you sure?', 'Why?', 'Tell me more'). DO NOT lose context of the conversation.");
        sb.AppendLine("CRITICAL RULE 3: Use the Database Context below as your source of truth for specific Safarni travel recommendations.");
        sb.AppendLine("CRITICAL RULE 4: If the user asks a general geographic, historical, or travel-related question (e.g., 'What is the capital of X?'), you MAY use your own accurate general knowledge if it's not in the Database Context.");
        sb.AppendLine("CRITICAL RULE 5: Be extremely accurate with geography. (e.g., The capital of Turkey is Ankara, not Istanbul. The capital of UAE is Abu Dhabi, not Dubai). Double-check your facts before answering.");
        sb.AppendLine("CRITICAL RULE 6: NEVER mention 'Database Context', 'System', or 'AI model' to the user. Speak naturally as Safarni AI.");

        sb.AppendLine("CRITICAL RULE 7: If the user asks about ANY non-travel topic (programming, math, science, sports, etc.), you MUST politely decline to answer AND YOU MUST NOT provide the answer or any information related to their non-travel question under any circumstances.");
        sb.AppendLine("Answer strictly in Arabic with a warm, helpful tone.");
        sb.AppendLine("If the user asks about ANY non-travel topic (programming, math, science, general news, etc.), politely decline in the same language as the user's latest message, stating that you are an AI travel assistant for Safarni and cannot answer non-travel questions.");
        sb.AppendLine("If the user sends a greeting (e.g., hello, how are you), respond politely and ask how you can help them with their travel plans.");
        AppendLanguageInstruction(sb, userMessage);
        sb.AppendLine();
        sb.AppendLine("Database Context:");
        
        if (contextChunks == null || contextChunks.Count == 0)
        {
            sb.AppendLine("(No context available - use general travel knowledge if appropriate)");
        }
        else
        {
            int index = 1;
            foreach (var chunk in contextChunks)
            {
                sb.AppendLine($"[Document {index}]");
                sb.AppendLine($"Entity Type: {chunk.EntityType}");
                if (!string.IsNullOrWhiteSpace(chunk.City)) sb.AppendLine($"City: {chunk.City}");
                if (!string.IsNullOrWhiteSpace(chunk.Country)) sb.AppendLine($"Country: {chunk.Country}");
                sb.AppendLine($"Content: {chunk.Content}");
                sb.AppendLine();
                index++;
            }
        }

        return sb.ToString();
    }

    public string BuildSystemPromptWithPlaces(List<DocumentChunk>? contextChunks, List<PlaceDetails>? places, ChatIntent intent, string userMessage)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are Safarni AI, a specialized intelligent travel and tourism assistant.");
        sb.AppendLine("CRITICAL RULE 1: Your primary goal is to help users with travel, tourism, and destination information.");
        sb.AppendLine("CRITICAL RULE 2: You MUST use the provided Chat History to understand conversational follow-up questions (e.g., 'Are you sure?', 'Why?', 'Tell me more'). DO NOT lose context of the conversation.");
        sb.AppendLine("CRITICAL RULE 3: Use the Google Places data below as your ONLY source of factual truth for these specific places.");
        sb.AppendLine("CRITICAL RULE 4: NEVER mention 'Google Places', 'Database Context', 'System', or 'AI model' to the user. Speak naturally as Safarni AI.");
        sb.AppendLine("CRITICAL RULE 5: If the user asks about ANY non-travel topic, you MUST politely decline to answer AND YOU MUST NOT provide the answer or any information related to their non-travel question under any circumstances.");
        sb.AppendLine("Answer strictly in Arabic.");
        sb.AppendLine("If the user asks about ANY other topic, you MUST politely decline in the same language as the user's latest message, stating that you only answer travel-related questions.");
        AppendLanguageInstruction(sb, userMessage);
        sb.AppendLine("Never return raw JSON.");
        sb.AppendLine("Format links as markdown links, not plain URLs.");
        sb.AppendLine();

        if (places != null && places.Count > 0)
        {
            sb.AppendLine("### REAL-TIME GOOGLE PLACES DATA:");
            sb.AppendLine("Present the place recommendations clearly containing:");
            sb.AppendLine("1. Name (اسم المكان)");
            sb.AppendLine("2. Rating (التقييم - e.g. ⭐ 4.5/5)");
            sb.AppendLine("3. Address (العنوان)");
            sb.AppendLine("4. Short Description (وصف قصير)");
            sb.AppendLine("5. Google Maps Link (رابط خرائط جوجل) - formatted strictly as [عرض المكان على الخريطة](URL)");
            sb.AppendLine();
            sb.AppendLine("Places Data:");

            int pIdx = 1;
            foreach (var p in places)
            {
                sb.AppendLine($"Place #{pIdx}:");
                sb.AppendLine($"  Name: {p.Name}");
                sb.AppendLine($"  Rating: {(p.Rating.HasValue ? p.Rating.Value.ToString("0.0") + " / 5 (" + (p.UserRatingsTotal ?? 0) + " تقييم)" : "N/A")}");
                sb.AppendLine($"  Address: {p.Address}");
                sb.AppendLine($"  Description: {p.Description}");
                sb.AppendLine($"  Google Maps Link: [عرض المكان على الخريطة]({p.GoogleMapsLink})");
                sb.AppendLine();
                pIdx++;
            }

            sb.AppendLine("INSTRUCTION: Provide a clear, natural recommendation of the Google Places results listed above. Keep all place text concise and enclosed neatly within a unified markdown response.");
        }
        else
        {
            sb.AppendLine("No Google Places data available. Reply ONLY with: \"عفواً، لا يمكنني العثور على هذه المعلومات عبر Google Places.\"");
        }

        return sb.ToString();
    }

    public string BuildUserPrompt(string question)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Question:");
        sb.AppendLine(question);
        sb.AppendLine();
        sb.AppendLine(IsMostlyEnglish(question)
            ? "Answer language: English only. Do not answer in Arabic."
            : "Answer language: Arabic only.");
        sb.AppendLine();
        sb.Append("Answer:");
        return sb.ToString();
    }

    public string BuildKnowledgeNotFoundMessage(string userMessage)
    {
        return IsMostlyEnglish(userMessage)
            ? "Sorry, I couldn't find this information in my knowledge base or through Google Places."
            : "\u0639\u0641\u0648\u0627\u064b\u060c \u0644\u0627 \u064a\u0645\u0643\u0646\u0646\u064a \u0627\u0644\u0639\u062b\u0648\u0631 \u0639\u0644\u0649 \u0647\u0630\u0647 \u0627\u0644\u0645\u0639\u0644\u0648\u0645\u0627\u062a \u0641\u064a \u0642\u0627\u0639\u062f\u0629 \u0627\u0644\u0645\u0639\u0631\u0641\u0629 \u0627\u0644\u062e\u0627\u0635\u0629 \u0628\u064a \u0648\u0644\u0627 \u0639\u0628\u0631 Google Places.";
    }

    public string GetGooglePlacesLanguageCode(string userMessage)
    {
        return IsMostlyEnglish(userMessage) ? "en" : "ar";
    }

    private static void AppendLanguageInstruction(StringBuilder sb, string userMessage)
    {
        if (IsMostlyEnglish(userMessage))
        {
            sb.AppendLine("LANGUAGE RULE: The user's latest message is primarily English. Answer strictly in English with a warm, helpful tone. This overrides conversation history and the language of any database context. If the database context is Arabic, translate the relevant facts into English.");
            return;
        }

        sb.AppendLine("LANGUAGE RULE: The user's latest message is primarily Arabic. Answer strictly in Arabic with a warm, helpful tone. This overrides conversation history and the language of any database context.");
    }

    private static bool IsMostlyEnglish(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var arabicCount = text.Count(c => c >= '\u0600' && c <= '\u06FF');
        var latinCount = text.Count(c => (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'));

        return latinCount > arabicCount;
    }
}
