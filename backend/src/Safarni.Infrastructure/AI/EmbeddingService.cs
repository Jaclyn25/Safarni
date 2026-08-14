using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ChatBotApi.Application.Common.DTOs;
using ChatBotApi.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ChatBotApi.Infrastructure.AI;

public class EmbeddingService : IEmbeddingService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly IAIProvider _aiProvider;
    private readonly ILogger<EmbeddingService> _logger;

    public EmbeddingService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IAIProvider aiProvider,
        ILogger<EmbeddingService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _aiProvider = aiProvider;
        _logger = logger;
    }

    private string ApiKey => Environment.GetEnvironmentVariable("OPENROUTER_API_KEY") 
                          ?? _configuration["OpenRouter:ApiKey"] 
                          ?? throw new InvalidOperationException("OpenRouter API key is not configured.");
    
    private string BaseUrl => _configuration["OpenRouter:BaseUrl"] ?? "https://openrouter.ai/api/v1";
    private string EmbeddingModel => _configuration["OpenRouter:EmbeddingModel"] ?? "nvidia/nemotron-3-embed-1b:free";
    private int? EmbeddingDimensions => int.TryParse(
        _configuration["OpenRouter:EmbeddingDimensions"],
        out var dimensions)
            ? dimensions
            : null;

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
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

            var embeddings = await GetEmbeddingsFromOpenRouterAsync(
                new List<string> { processedText },
                _configuration["OpenRouter:QueryInputType"] ?? "query",
                cancellationToken);
            return embeddings.FirstOrDefault() ?? Array.Empty<float>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while generating OpenRouter embedding.");
            throw new InvalidOperationException("حدث خطأ أثناء توليد الـ Vector للرسالة عبر OpenRouter.", ex);
        }
    }

    public async Task<List<float[]>> GenerateEmbeddingsAsync(List<string> texts, CancellationToken cancellationToken = default)
    {
        if (texts == null || texts.Count == 0)
        {
            return new List<float[]>();
        }

        try
        {
            return await GetEmbeddingsFromOpenRouterAsync(
                texts,
                _configuration["OpenRouter:DocumentInputType"] ?? "passage",
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while generating batch OpenRouter embeddings.");
            throw new InvalidOperationException("حدث خطأ أثناء توليد الـ Vectors للدفعة عبر OpenRouter.", ex);
        }
    }

    private async Task<List<float[]>> GetEmbeddingsFromOpenRouterAsync(
        List<string> texts,
        string inputType,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating OpenRouter embeddings for batch of {Count} texts using model {Model}", texts.Count, EmbeddingModel);

        int maxRetries = 3;
        int currentAttempt = 0;

        while (true)
        {
            using var client = _httpClientFactory.CreateClient();
            
            var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl.TrimEnd('/')}/embeddings");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);
            
            var requestBody = new Dictionary<string, object>
            {
                ["model"] = EmbeddingModel,
                ["input"] = texts,
                ["input_type"] = inputType,
                ["encoding_format"] = "float"
            };

            if (EmbeddingDimensions.HasValue)
            {
                requestBody["dimensions"] = EmbeddingDimensions.Value;
            }

            request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            try
            {
                var response = await client.SendAsync(request, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    if (currentAttempt > 0)
                    {
                        _logger.LogInformation("Embedding request succeeded after retry.");
                    }

                    var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var responseData = await response.Content.ReadFromJsonAsync<OpenRouterEmbeddingResponse>(jsonOptions, cancellationToken);

                    if (responseData?.Data == null || responseData.Data.Count == 0)
                    {
                        _logger.LogError("OpenRouter response did not contain embeddings.");
                        throw new InvalidOperationException("OpenRouter response did not contain embeddings.");
                    }

                    var results = responseData.Data.OrderBy(d => d.Index).Select(d => d.Embedding).ToList();

                    if (results.Count != texts.Count)
                    {
                        throw new InvalidOperationException(
                            $"OpenRouter returned {results.Count} embeddings for {texts.Count} inputs.");
                    }

                    if (EmbeddingDimensions.HasValue &&
                        results.Any(embedding => embedding.Length != EmbeddingDimensions.Value))
                    {
                        throw new InvalidOperationException(
                            $"Embedding model returned a vector dimension different from the configured {EmbeddingDimensions.Value} values.");
                    }
                    
                    if (results.Count > 0 && results[0] != null)
                    {
                        _logger.LogInformation("Successfully generated OpenRouter embeddings. Count: {Count}, Dimension: {Dim}", results.Count, results[0].Length);
                    }

                    return results;
                }

                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                
                // Non-retryable errors
                if (response.StatusCode == HttpStatusCode.BadRequest || 
                    response.StatusCode == HttpStatusCode.Unauthorized || 
                    response.StatusCode == HttpStatusCode.Forbidden)
                {
                    if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
                    {
                        _logger.LogError("OpenRouter API key is invalid or unauthorized.");
                        throw new UnauthorizedAccessException("The OpenRouter API key is invalid or unauthorized.");
                    }
                    
                    _logger.LogError("OpenRouter returned non-retryable error {StatusCode}. Body: {Body}", response.StatusCode, errorContent);
                    throw new HttpRequestException($"OpenRouter returned {response.StatusCode} error.", null, response.StatusCode);
                }

                // Retryable errors
                if (currentAttempt >= maxRetries)
                {
                    _logger.LogError("OpenRouter returned error {StatusCode}. Body: {Body}. Max retries ({MaxRetries}) reached.", response.StatusCode, errorContent, maxRetries);
                    throw new HttpRequestException($"OpenRouter API rate limit exceeded or server error.", null, response.StatusCode);
                }

                currentAttempt++;
                
                TimeSpan delay = TimeSpan.FromSeconds(Math.Pow(2, currentAttempt)); // 2s, 4s, 8s

                if (response.Headers.RetryAfter?.Delta.HasValue == true)
                {
                    delay = response.Headers.RetryAfter.Delta.Value;
                }
                else if (response.Headers.RetryAfter?.Date.HasValue == true)
                {
                    var calculatedDelay = response.Headers.RetryAfter.Date.Value - DateTimeOffset.UtcNow;
                    if (calculatedDelay > TimeSpan.Zero) delay = calculatedDelay;
                }

                _logger.LogWarning("Embedding request received {StatusCode}. Retrying attempt {Attempt}/{MaxRetries} after {DelaySeconds} seconds.", (int)response.StatusCode, currentAttempt, maxRetries, delay.TotalSeconds);
                
                await Task.Delay(delay, cancellationToken);
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                if (ex is TaskCanceledException && cancellationToken.IsCancellationRequested)
                {
                    throw; // Respect actual cancellation
                }

                if (ex is HttpRequestException httpEx && httpEx.StatusCode.HasValue)
                {
                    var code = httpEx.StatusCode.Value;
                    if (code != HttpStatusCode.TooManyRequests && (int)code < 500)
                    {
                        throw; // Do not retry 4xx errors (except 429)
                    }
                }

                if (currentAttempt >= maxRetries)
                {
                    _logger.LogError(ex, "Network or server error during OpenRouter request. Max retries ({MaxRetries}) reached.", maxRetries);
                    throw;
                }

                currentAttempt++;
                TimeSpan delay = TimeSpan.FromSeconds(Math.Pow(2, currentAttempt)); // 2s, 4s, 8s
                _logger.LogWarning(ex, "Embedding request encountered network or server error. Retrying attempt {Attempt}/{MaxRetries} after {DelaySeconds} seconds.", currentAttempt, maxRetries, delay.TotalSeconds);
                await Task.Delay(delay, cancellationToken);
            }
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

    private class OpenRouterEmbeddingResponse
    {
        public List<OpenRouterEmbeddingData>? Data { get; set; }
    }

    private class OpenRouterEmbeddingData
    {
        public int Index { get; set; }
        public float[] Embedding { get; set; } = Array.Empty<float>();
    }
}
