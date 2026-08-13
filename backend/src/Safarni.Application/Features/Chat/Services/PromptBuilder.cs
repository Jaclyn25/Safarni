using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChatBotApi.Application.Common.Interfaces;
using ChatBotApi.Application.Common.Models.GooglePlaces;
using ChatBotApi.Domain.Entities;

namespace ChatBotApi.Application.Features.Chat.Services;

public class PromptBuilder : IPromptBuilder
{
    private string DetermineResponseLanguage(string userMessage)
    {
        if (string.IsNullOrWhiteSpace(userMessage)) return "English";
        return userMessage.Any(c => c >= '\u0600' && c <= '\u06FF') ? "Arabic" : "English";
    }

    public string BuildDirectSystemPrompt(ChatIntent intent, string userMessage)
    {
        var lang = DetermineResponseLanguage(userMessage);
        var sb = new StringBuilder();
        sb.AppendLine("You are Safarni AI, a specialized intelligent travel and tourism assistant.");
        sb.AppendLine($"Answer strictly in {lang} with a warm, concise, helpful tone.");
        sb.AppendLine($"CRITICAL RULE: You MUST respond in {lang}, regardless of the language of any context or previous messages.");
        sb.AppendLine("Never return raw JSON.");

        if (intent == ChatIntent.Social)
        {
            sb.AppendLine("The user is making small talk or greeting you. Respond naturally, then invite them to ask for help with travel, destinations, itineraries, hotels, attractions, or trip planning.");
        }
        else
        {
            sb.AppendLine($"If the user asks about anything outside travel and tourism, politely decline in {lang} and explain that you can only help with Safarni travel-related questions.");
        }

        return sb.ToString();
    }

    public string BuildSystemPrompt(List<DocumentChunk> contextChunks, string userMessage)
    {
        var lang = DetermineResponseLanguage(userMessage);
        var sb = new StringBuilder();
        sb.AppendLine("You are Safarni AI, a specialized intelligent travel and tourism assistant.");
        sb.AppendLine("CRITICAL RULE 1: You must ONLY answer questions that are directly related to tourism, travel, destinations, or trip planning.");
        sb.AppendLine($"If the user asks about ANY other topic (programming, math, science, general news, etc.), you MUST politely decline to answer in {lang}, stating that you are an AI travel assistant for Safarni and cannot answer non-travel questions.");
        sb.AppendLine("If the user sends a greeting (e.g., hello, how are you), respond politely and ask how you can help them with their travel plans.");
        sb.AppendLine($"Answer strictly in {lang} with a warm, helpful tone.");
        sb.AppendLine($"CRITICAL RULE 2: You MUST respond in {lang}, regardless of the language of the retrieved context or previous messages.");
        sb.AppendLine("CRITICAL RULE 3: Use the Database Context below as your ONLY source of factual truth for travel recommendations and details.");
        sb.AppendLine("If the Database Context is empty or does not contain the answer to the user's travel question, DO NOT make up information or use general knowledge. Instead, politely state that you do not have this information in your database.");
        sb.AppendLine();
        sb.AppendLine("Database Context:");

        if (contextChunks == null || contextChunks.Count == 0)
        {
            sb.AppendLine("(No Safarni-specific context found. Use general travel knowledge if the question is in scope.)");
        }
        else
        {
            var index = 1;
            foreach (var chunk in contextChunks)
            {
                sb.AppendLine($"[Document {index}]");
                sb.AppendLine($"Entity Type: {chunk.EntityType}");
                if (!string.IsNullOrWhiteSpace(chunk.City))
                {
                    sb.AppendLine($"City: {chunk.City}");
                }

                if (!string.IsNullOrWhiteSpace(chunk.Country))
                {
                    sb.AppendLine($"Country: {chunk.Country}");
                }

                sb.AppendLine($"Content: {chunk.Content}");
                sb.AppendLine();
                index++;
            }
        }

        return sb.ToString();
    }

    public string BuildSystemPromptWithPlaces(List<DocumentChunk>? contextChunks, List<PlaceDetails>? places, ChatIntent intent, string userMessage)
    {
        var lang = DetermineResponseLanguage(userMessage);
        var sb = new StringBuilder();
        sb.AppendLine("You are Safarni AI, a specialized intelligent travel and tourism assistant.");
        sb.AppendLine("CRITICAL RULE 1: You must ONLY answer questions that are directly related to tourism, travel, destinations, or trip planning.");
        sb.AppendLine($"If the user asks about ANY other topic, you MUST politely decline to answer in {lang}, stating that you only answer travel-related questions.");
        sb.AppendLine($"Answer strictly in {lang}.");
        sb.AppendLine($"CRITICAL RULE 2: You MUST respond in {lang}, regardless of the language of any context or previous messages.");
        sb.AppendLine("Never return raw JSON.");
        sb.AppendLine("Format links as markdown links, not plain URLs.");
        sb.AppendLine("CRITICAL RULE 3: Use the Google Places data below as your ONLY source of factual truth for these places.");
        sb.AppendLine();

        if (contextChunks != null && contextChunks.Count > 0)
        {
            sb.AppendLine("Additional Travel Context:");
            foreach (var chunk in contextChunks.Take(3))
            {
                sb.AppendLine($"- {chunk.Content}");
            }

            sb.AppendLine();
        }

        if (places != null && places.Count > 0)
        {
            sb.AppendLine("Place Data:");
            var index = 1;
            foreach (var place in places)
            {
                sb.AppendLine($"Place #{index}:");
                sb.AppendLine($"Name: {place.Name}");
                sb.AppendLine($"Rating: {(place.Rating.HasValue ? $"{place.Rating.Value:0.0}/5 ({place.UserRatingsTotal ?? 0} reviews)" : "N/A")}");
                sb.AppendLine($"Address: {place.Address}");
                sb.AppendLine($"Description: {place.Description}");
                sb.AppendLine($"Google Maps Link: [View on map]({place.GoogleMapsLink})");
                sb.AppendLine();
                index++;
            }

            sb.AppendLine("Present the results naturally. Include name, rating when available, address, a short reason to visit, and the map link.");
        }
        else
        {
            sb.AppendLine("No place data is available. Give a helpful travel answer from general knowledge and say that live place details are unavailable right now.");
        }

        return sb.ToString();
    }

    public string BuildUserPrompt(string question)
    {
        var sb = new StringBuilder();
        sb.AppendLine("User question:");
        sb.AppendLine(question);
        sb.AppendLine();
        sb.Append("Answer:");
        return sb.ToString();
    }

    public string GetGooglePlacesLanguageCode(string userMessage)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
        {
            return "en";
        }

        return userMessage.Any(c => c >= '\u0600' && c <= '\u06FF') ? "ar" : "en";
    }
}
