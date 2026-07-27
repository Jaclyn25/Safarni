using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChatBotApi.Application.Common.DTOs;
using ChatBotApi.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using SmartComponents.LocalEmbeddings;

namespace ChatBotApi.Infrastructure.AI;

public class EmbeddingService : IEmbeddingService
{
    private readonly LocalEmbedder _embedder;
    private readonly IAIProvider _aiProvider;
    private readonly ILogger<EmbeddingService> _logger;

    public EmbeddingService(
        LocalEmbedder embedder,
        IAIProvider aiProvider,
        ILogger<EmbeddingService> logger)
    {
        _embedder = embedder;
        _aiProvider = aiProvider;
        _logger = logger;
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogWarning("GenerateEmbeddingAsync received null or empty text.");
            throw new ArgumentException("Text cannot be null or empty for embedding generation.", nameof(text));
        }

        try
        {
            var processedText = text;
            if (IsArabic(text))
            {
                processedText = await TranslateToEnglishAsync(text);
            }

            _logger.LogInformation("Generating local embedding for text (length: {Length}) using SmartComponents", processedText.Length);
            var embedding = _embedder.Embed<EmbeddingF32>(processedText);
            var vector = embedding.Values.ToArray();
            
            _logger.LogInformation("Successfully generated local embedding (dimension: {Dim})", vector.Length);
            return vector;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while generating local embedding.");
            throw new InvalidOperationException("حدث خطأ أثناء توليد الـ Vector للرسالة محلياً.", ex);
        }
    }

    private bool IsArabic(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        foreach (var c in text)
        {
            if (c >= 0x0600 && c <= 0x06FF)
                return true;
        }
        return false;
    }

    private async Task<string> TranslateToEnglishAsync(string text)
    {
        try
        {
            _logger.LogInformation("Translating Arabic query to English: '{Text}'", text);
            
            var systemPrompt = "You are a precise translator. Translate the user's travel/tourism search query from Arabic to English. Return ONLY the final translated English text. Do not include any notes, explanations, or quotes.";
            var translated = await _aiProvider.GetChatCompletionAsync(
                systemPrompt,
                new List<ChatMessageDto>(),
                text
            );
            
            var cleanResult = translated.Trim().Trim('"', '\'');
            _logger.LogInformation("Successfully translated query: '{Result}'", cleanResult);
            return cleanResult;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to translate query to English, falling back to original query.");
            return text;
        }
    }

    public Task<List<float[]>> GenerateEmbeddingsAsync(List<string> texts)
    {
        if (texts == null || texts.Count == 0)
        {
            return Task.FromResult(new List<float[]>());
        }

        try
        {
            _logger.LogInformation("Generating local embeddings for batch of {Count} texts using SmartComponents", texts.Count);
            var results = new List<float[]>();
            foreach (var text in texts)
            {
                var embedding = _embedder.Embed<EmbeddingF32>(text);
                results.Add(embedding.Values.ToArray());
            }
            
            _logger.LogInformation("Successfully generated local embeddings for {Count} texts", results.Count);
            return Task.FromResult(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while generating batch local embeddings.");
            throw new InvalidOperationException("حدث خطأ أثناء توليد الـ Vectors للدفعة محلياً.", ex);
        }
    }
}
