using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using ChatBotApi.Application.Common.DTOs;
using ChatBotApi.Application.Common.Exceptions;
using ChatBotApi.Application.Common.Interfaces;
using ChatBotApi.Application.Common.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ChatBotApi.Infrastructure.AI;

public class GroqProvider : IAIProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GroqProvider> _logger;

    public GroqProvider(
        IHttpClientFactory httpClientFactory, 
        IConfiguration configuration, 
        ILogger<GroqProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> GetChatCompletionAsync(
        string systemPrompt, 
        List<ChatMessageDto> history, 
        string userMessage, 
        CancellationToken cancellationToken = default,
        AICompletionOptions? options = null)
    {
        var apiKey = _configuration["Groq:ApiKey"] ?? Environment.GetEnvironmentVariable("GROQ_API_KEY");
        var model = _configuration["Groq:Model"] ?? "llama-3.3-70b-versatile";
        var configuredTemperature = double.TryParse(_configuration["Groq:Temperature"], out var temp) ? temp : 0.65;
        var configuredMaxTokens = int.TryParse(_configuration["Groq:MaxTokens"], out var tokens) ? tokens : 1400;
        var configuredTopP = double.TryParse(_configuration["Groq:TopP"], out var p) ? p : 0.9;
        var temperature = options?.Temperature ?? configuredTemperature;
        var maxTokens = options?.MaxTokens ?? configuredMaxTokens;
        var topP = options?.TopP ?? configuredTopP;
        var timeoutSeconds = int.TryParse(_configuration["Groq:TimeoutSeconds"], out var sec) ? sec : 30;

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogError("Groq API Key is not configured.");
            throw new InvalidOperationException("إعدادات المزود غير صحيحة: مفتاح API غير متوفر.");
        }

        var messages = new List<object>
        {
            new { role = "system", content = systemPrompt }
        };

        foreach (var msg in history)
        {
            messages.Add(new { role = msg.Role, content = msg.Content });
        }

        messages.Add(new { role = "user", content = userMessage });

        var requestBody = new
        {
            model = model,
            messages = messages,
            temperature = temperature,
            max_tokens = maxTokens,
            top_p = topP
        };

        var jsonRequestBody = JsonSerializer.Serialize(requestBody);
        var promptSize = jsonRequestBody.Length;
        _logger.LogInformation("Sending chat completion request. Prompt size: {PromptSize} characters. Model: {Model}.", promptSize, model);

        using var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = new StringContent(jsonRequestBody, Encoding.UTF8, "application/json");

        var stopwatch = Stopwatch.StartNew();
        HttpResponseMessage response;

        try
        {
            response = await client.SendAsync(request, cancellationToken);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Request to Groq API timed out after {Timeout} seconds.", timeoutSeconds);
            throw new TimeoutException("انتهت مهلة الاتصال بمزود الذكاء الاصطناعي. الرجاء المحاولة لاحقاً.", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error occurred when contacting Groq API.");
            throw new HttpRequestException("فشل الاتصال بالإنترنت أثناء التواصل مع خادم الذكاء الاصطناعي.", ex);
        }

        stopwatch.Stop();
        var elapsedMs = stopwatch.ElapsedMilliseconds;

        _logger.LogInformation("Received Groq response in {ElapsedMs} ms. Status code: {StatusCode}", elapsedMs, response.StatusCode);

        if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
        {
            _logger.LogError("Groq API Key is unauthorized. Status: {StatusCode}", response.StatusCode);
            throw new UnauthorizedAccessException("مفتاح API الخاص بمزود الذكاء الاصطناعي غير صالح أو غير مصرح به.");
        }

        if (response.StatusCode == (HttpStatusCode)429)
        {
            _logger.LogWarning("Groq API rate limit exceeded.");
            throw new AIProviderRateLimitException(
                "وصلت المحادثة إلى الحد المتاح حاليًا. ابدأ محادثة جديدة للمتابعة.");
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Groq API returned failure status code: {StatusCode}. Body: {Body}", response.StatusCode, errorContent);
            throw new HttpRequestException("حدث خطأ في خدمة الذكاء الاصطناعي. يرجى المحاولة لاحقاً.");
        }

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var deserialized = JsonSerializer.Deserialize<GroqResponseJson>(responseContent, jsonOptions);

        var reply = deserialized?.Choices?[0]?.Message?.Content;
        if (string.IsNullOrWhiteSpace(reply))
        {
            _logger.LogError("Groq response does not contain choices message content. Body: {Body}", responseContent);
            throw new InvalidOperationException("تلقينا رداً فارغاً من مزود الذكاء الاصطناعي.");
        }

        if (deserialized?.Usage != null)
        {
            _logger.LogInformation(
                "Token usage: Prompt: {PromptTokens}, Completion: {CompletionTokens}, Total: {TotalTokens}",
                deserialized.Usage.PromptTokens,
                deserialized.Usage.CompletionTokens,
                deserialized.Usage.TotalTokens
            );
        }

        return reply;
    }

    private class GroqResponseJson
    {
        public List<GroqChoiceJson>? Choices { get; set; }
        public GroqUsageJson? Usage { get; set; }
    }

    private class GroqChoiceJson
    {
        public GroqMessageJson? Message { get; set; }
    }

    private class GroqMessageJson
    {
        public string? Content { get; set; }
    }

    private class GroqUsageJson
    {
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }

        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }

        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }
    }
}
