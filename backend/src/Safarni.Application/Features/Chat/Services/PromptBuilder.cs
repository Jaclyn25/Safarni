using System.Collections.Generic;
using System.Text;
using ChatBotApi.Application.Common.Interfaces;
using ChatBotApi.Application.Common.Models.GooglePlaces;
using ChatBotApi.Domain.Entities;

namespace ChatBotApi.Application.Features.Chat.Services;

public class PromptBuilder : IPromptBuilder
{
    public string BuildSystemPrompt(List<DocumentChunk> contextChunks)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are Safarni AI, a specialized intelligent travel and tourism assistant.");
        sb.AppendLine("CRITICAL RULE 1: You must ONLY answer questions that are directly related to tourism, travel, destinations, or trip planning.");
        sb.AppendLine("If the user asks about ANY other topic (programming, math, science, general news, etc.), you MUST politely decline to answer in Arabic, stating that you are an AI travel assistant for Safarni and cannot answer non-travel questions.");
        sb.AppendLine("If the user sends a greeting (e.g., hello, how are you), respond politely and ask how you can help them with their travel plans.");
        sb.AppendLine("Answer strictly in Arabic with a warm, helpful tone.");
        sb.AppendLine("CRITICAL RULE 2: Use the Database Context below as your ONLY source of factual truth for travel recommendations and details.");
        sb.AppendLine("If the Database Context is empty or does not contain the answer to the user's travel question, DO NOT make up information or use general knowledge. Instead, politely state that you do not have this information in your database.");
        sb.AppendLine();
        sb.AppendLine("Database Context:");
        
        if (contextChunks == null || contextChunks.Count == 0)
        {
            sb.AppendLine("(No context available)");
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

    public string BuildSystemPromptWithPlaces(List<DocumentChunk>? contextChunks, List<PlaceDetails>? places, ChatIntent intent)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are Safarni AI, a specialized intelligent travel and tourism assistant.");
        sb.AppendLine("CRITICAL RULE 1: You must ONLY answer questions that are directly related to tourism, travel, destinations, or trip planning.");
        sb.AppendLine("If the user asks about ANY other topic, you MUST politely decline to answer in Arabic, stating that you only answer travel-related questions.");
        sb.AppendLine("Answer strictly in Arabic.");
        sb.AppendLine("Never return raw JSON.");
        sb.AppendLine("Format links as markdown links, not plain URLs.");
        sb.AppendLine("CRITICAL RULE 2: Use the Google Places data below as your ONLY source of factual truth for these places.");
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
        sb.Append("Answer:");
        return sb.ToString();
    }
}
