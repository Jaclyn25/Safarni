using System.Collections.Generic;
using ChatBotApi.Application.Common.Models.GooglePlaces;
using ChatBotApi.Domain.Entities;

namespace ChatBotApi.Application.Common.Interfaces;

public interface IPromptBuilder
{
    string BuildSystemPrompt(List<DocumentChunk> contextChunks);
    string BuildSystemPromptWithPlaces(List<DocumentChunk>? contextChunks, List<PlaceDetails>? places, ChatIntent intent);
    string BuildUserPrompt(string question);
}
