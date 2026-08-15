using System.Collections.Generic;
using ChatBotApi.Application.Common.Models.GooglePlaces;
using ChatBotApi.Domain.Entities;

namespace ChatBotApi.Application.Common.Interfaces;

public interface IPromptBuilder
{
    string BuildSystemPrompt(List<DocumentChunk> contextChunks, string userMessage);
    string BuildSystemPromptWithPlaces(List<DocumentChunk>? contextChunks, List<PlaceDetails>? places, ChatIntent intent, string userMessage);
    string BuildDirectSystemPrompt(ChatIntent intent, string userMessage);
    string BuildTripPlanSystemPrompt(string userMessage, string groundingContext);
    string BuildUserPrompt(string question);
    string GetGooglePlacesLanguageCode(string userMessage);
}
