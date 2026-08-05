using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ChatBotApi.Application.Common.DTOs;
using ChatBotApi.Application.Common.Exceptions;
using ChatBotApi.Application.Common.Interfaces;
using ChatBotApi.Application.Common.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ChatBotApi.Infrastructure.AI;

/// <summary>
/// Gemini provider used ONLY for itinerary generation.
/// The normal Safarni chatbot continues to resolve IAIProvider to GroqProvider.
/// </summary>
public sealed class GeminiItineraryAIProvider : IItineraryAIProvider
{
    private const string ConfigurationSection = "GeminiItinerary";
    private const string DefaultBaseUrl =
        "https://generativelanguage.googleapis.com/v1beta";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GeminiItineraryAIProvider> _logger;

    public GeminiItineraryAIProvider(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<GeminiItineraryAIProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public string ProviderName => "GeminiItinerary";

    public string ModelName =>
        _configuration[$"{ConfigurationSection}:Model"] ??
        "gemini-2.5-flash";

    public Task<string> GetChatCompletionAsync(
        string systemPrompt,
        List<ChatMessageDto> history,
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        return GetChatCompletionCoreAsync(
            systemPrompt,
            history,
            userMessage,
            options: null,
            cancellationToken);
    }

    public Task<string> GetChatCompletionAsync(
        string systemPrompt,
        List<ChatMessageDto> history,
        string userMessage,
        AICompletionOptions options,
        CancellationToken cancellationToken = default)
    {
        return GetChatCompletionCoreAsync(
            systemPrompt,
            history,
            userMessage,
            options,
            cancellationToken);
    }

    // Itinerary generation is non-streaming. This implementation keeps the
    // provider compatible if IItineraryAIProvider exposes the streaming member.
    public async IAsyncEnumerable<string> StreamChatCompletionAsync(
        string systemPrompt,
        List<ChatMessageDto> history,
        string userMessage,
        [System.Runtime.CompilerServices.EnumeratorCancellation]
        CancellationToken cancellationToken = default)
    {
        var content = await GetChatCompletionCoreAsync(
            systemPrompt,
            history,
            userMessage,
            options: null,
            cancellationToken);

        yield return content;
    }

    private async Task<string> GetChatCompletionCoreAsync(
        string systemPrompt,
        IReadOnlyCollection<ChatMessageDto> history,
        string userMessage,
        AICompletionOptions? options,
        CancellationToken cancellationToken)
    {
        var settings = GetSettings();
        var requestBody = CreateRequestBody(
            settings,
            systemPrompt,
            history,
            userMessage,
            options);

        using var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{settings.BaseUrl}/models/{Uri.EscapeDataString(settings.Model)}:generateContent");

        request.Headers.TryAddWithoutValidation(
            "x-goog-api-key",
            settings.ApiKey);

        request.Content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json");

        var startedAt = DateTimeOffset.UtcNow;

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(
                request,
                HttpCompletionOption.ResponseContentRead,
                cancellationToken);
        }
        catch (TaskCanceledException exception)
            when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(
                exception,
                "Gemini itinerary request timed out after {TimeoutSeconds} seconds.",
                settings.TimeoutSeconds);

            throw new TimeoutException(
                "The itinerary AI provider request timed out.",
                exception);
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(
                exception,
                "Could not connect to the Gemini itinerary API.");

            throw new HttpRequestException(
                "Could not connect to the itinerary AI provider.",
                exception);
        }

        using (response)
        {
            var body = await response.Content.ReadAsStringAsync(
                cancellationToken);

            _logger.LogInformation(
                "Received Gemini itinerary response in {ElapsedMs} ms. Status code: {StatusCode}",
                (DateTimeOffset.UtcNow - startedAt).TotalMilliseconds,
                response.StatusCode);

            await EnsureSuccessAsync(
                response,
                body,
                cancellationToken);

            GeminiGenerateContentResponse? result;
            try
            {
                result = JsonSerializer.Deserialize<GeminiGenerateContentResponse>(
                    body,
                    JsonOptions);
            }
            catch (JsonException exception)
            {
                _logger.LogError(
                    exception,
                    "Gemini returned malformed JSON. Body: {Body}",
                    body);

                throw new InvalidOperationException(
                    "The itinerary AI provider returned an invalid response.",
                    exception);
            }

            var finishReason = result?.Candidates?
                .FirstOrDefault()?.FinishReason;

            _logger.LogInformation(
                "Gemini itinerary finish reason: {FinishReason}.",
                string.IsNullOrWhiteSpace(finishReason)
                    ? "Unknown"
                    : finishReason);

            if (result?.UsageMetadata is { } usage)
            {
                _logger.LogInformation(
                    "Gemini itinerary token usage. Prompt: {PromptTokens}, Completion: {CompletionTokens}, Thinking: {ThinkingTokens}, Total: {TotalTokens}",
                    usage.PromptTokenCount,
                    usage.CandidatesTokenCount,
                    usage.ThoughtsTokenCount,
                    usage.TotalTokenCount);
            }

            if (string.Equals(
                    finishReason,
                    "MAX_TOKENS",
                    StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "Gemini stopped the itinerary because it reached the output token limit. RequestedMaxTokens: {RequestedMaxTokens}. ConfiguredMaxOutputTokens: {ConfiguredMaxOutputTokens}.",
                    options?.MaxTokens,
                    settings.MaxOutputTokens);

                throw new InvalidOperationException(
                    "The itinerary AI response was truncated because the output token limit was reached.");
            }

            var reply = ExtractText(result);

            if (string.IsNullOrWhiteSpace(reply))
            {
                var blockReason = result?.PromptFeedback?.BlockReason;

                _logger.LogError(
                    "Gemini itinerary response contained no usable text. FinishReason: {FinishReason}. BlockReason: {BlockReason}. Body: {Body}",
                    finishReason,
                    blockReason,
                    body);

                throw new InvalidOperationException(
                    "The itinerary AI provider returned an empty or blocked response.");
            }

            return reply.Trim();
        }
    }

    private GeminiSettings GetSettings()
    {
        var apiKey =
            Environment.GetEnvironmentVariable("GEMINI_ITINERARY_API_KEY") ??
            _configuration[$"{ConfigurationSection}:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "Gemini itinerary API key is not configured. " +
                "Set GEMINI_ITINERARY_API_KEY in your .env file.");
        }

        var baseUrl =
            _configuration[$"{ConfigurationSection}:BaseUrl"] ??
            DefaultBaseUrl;

        return new GeminiSettings(
            apiKey.Trim(),
            baseUrl.TrimEnd('/'),
            _configuration[$"{ConfigurationSection}:Model"] ??
                "gemini-2.5-flash",
            ParseDouble("Temperature", 0.2),
            ParseInt("MaxOutputTokens", 6000),
            ParseDouble("TopP", 0.9),
            ParseInt("ThinkingBudget", 0),
            ParseInt("TimeoutSeconds", 120));
    }

    private object CreateRequestBody(
        GeminiSettings settings,
        string systemPrompt,
        IReadOnlyCollection<ChatMessageDto> history,
        string userMessage,
        AICompletionOptions? options)
    {
        var contents = new List<object>();

        foreach (var message in history)
        {
            if (string.IsNullOrWhiteSpace(message.Content))
            {
                continue;
            }

            contents.Add(new
            {
                role = MapRole(message.Role),
                parts = new[]
                {
                    new { text = message.Content }
                }
            });
        }

        contents.Add(new
        {
            role = "user",
            parts = new[]
            {
                new { text = userMessage }
            }
        });

        var maxOutputTokens = options?.MaxTokens is > 0
            ? Math.Min(
                options.MaxTokens.Value,
                settings.MaxOutputTokens)
            : settings.MaxOutputTokens;

        var temperature = options?.Temperature is >= 0
            ? options.Temperature.Value
            : settings.Temperature;

        var topP = options?.TopP is > 0
            ? options.TopP.Value
            : settings.TopP;

        var generationConfig = new Dictionary<string, object?>
        {
            ["temperature"] = temperature,
            ["topP"] = topP,
            ["maxOutputTokens"] = maxOutputTokens,
            // Gemini 2.5 Flash supports thinkingBudget = 0. Keeping
            // thinking off leaves the dynamic token budget for visible JSON.
            ["thinkingConfig"] = new
            {
                thinkingBudget = settings.ThinkingBudget
            }
        };

        if (options?.ResponseFormat ==
            AICompletionResponseFormat.ItineraryJson)
        {
            generationConfig["responseMimeType"] =
                "application/json";
            generationConfig["responseJsonSchema"] =
                CreateItineraryResponseJsonSchema();
        }
        else
        {
            generationConfig["responseMimeType"] = "text/plain";
        }

        return new Dictionary<string, object?>
        {
            ["system_instruction"] = new
            {
                parts = new[]
                {
                    new { text = systemPrompt }
                }
            },
            ["contents"] = contents,
            ["generationConfig"] = generationConfig
        };
    }

    private static object CreateItineraryResponseJsonSchema()
    {
        var itemSchema = new Dictionary<string, object?>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object?>
            {
                ["startTime"] = new Dictionary<string, object?>
                {
                    ["type"] = "string"
                },
                ["endTime"] = new Dictionary<string, object?>
                {
                    ["type"] = "string"
                },
                ["title"] = new Dictionary<string, object?>
                {
                    ["type"] = "string"
                },
                ["description"] = new Dictionary<string, object?>
                {
                    ["type"] = new[] { "string", "null" }
                },
                ["location"] = new Dictionary<string, object?>
                {
                    ["type"] = new[] { "string", "null" }
                },
                ["placeReference"] = new Dictionary<string, object?>
                {
                    ["type"] = new[] { "string", "null" }
                },
                ["category"] = new Dictionary<string, object?>
                {
                    ["type"] = "string",
                    ["enum"] = new[]
                    {
                        "activity",
                        "meal",
                        "transport",
                        "accommodation",
                        "other"
                    }
                },
                ["estimatedCost"] = new Dictionary<string, object?>
                {
                    ["type"] = new[] { "number", "null" },
                    ["minimum"] = 0
                },
                ["orderIndex"] = new Dictionary<string, object?>
                {
                    ["type"] = "integer",
                    ["minimum"] = 0
                }
            },
            ["required"] = new[]
            {
                "startTime",
                "endTime",
                "title",
                "description",
                "location",
                "placeReference",
                "category",
                "estimatedCost",
                "orderIndex"
            },
            ["additionalProperties"] = false,
            ["propertyOrdering"] = new[]
            {
                "startTime",
                "endTime",
                "title",
                "description",
                "location",
                "placeReference",
                "category",
                "estimatedCost",
                "orderIndex"
            }
        };

        var daySchema = new Dictionary<string, object?>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object?>
            {
                ["dayNumber"] = new Dictionary<string, object?>
                {
                    ["type"] = "integer",
                    ["minimum"] = 1
                },
                ["date"] = new Dictionary<string, object?>
                {
                    ["type"] = "string"
                },
                ["title"] = new Dictionary<string, object?>
                {
                    ["type"] = "string"
                },
                ["notes"] = new Dictionary<string, object?>
                {
                    ["type"] = new[] { "string", "null" }
                },
                ["items"] = new Dictionary<string, object?>
                {
                    ["type"] = "array",
                    ["minItems"] = 1,
                    ["items"] = itemSchema
                }
            },
            ["required"] = new[]
            {
                "dayNumber",
                "date",
                "title",
                "notes",
                "items"
            },
            ["additionalProperties"] = false,
            ["propertyOrdering"] = new[]
            {
                "dayNumber",
                "date",
                "title",
                "notes",
                "items"
            }
        };

        return new Dictionary<string, object?>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object?>
            {
                ["days"] = new Dictionary<string, object?>
                {
                    ["type"] = "array",
                    ["minItems"] = 1,
                    ["items"] = daySchema
                }
            },
            ["required"] = new[] { "days" },
            ["additionalProperties"] = false,
            ["propertyOrdering"] = new[] { "days" }
        };
    }

    private async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        string body,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (response.IsSuccessStatusCode)
        {
            return;
        }

        if (response.StatusCode is
            HttpStatusCode.Unauthorized or
            HttpStatusCode.Forbidden ||
            IsInvalidApiKey(response.StatusCode, body))
        {
            _logger.LogError(
                "Gemini rejected the itinerary API key. Status: {StatusCode}. Body: {Body}",
                response.StatusCode,
                body);

            throw new UnauthorizedAccessException(
                "The itinerary AI provider API key is invalid or unauthorized.");
        }

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            var retryAfter = ResolveRetryAfter(response, body);

            _logger.LogWarning(
                "Gemini itinerary rate limit was exceeded. Retry after: {RetryAfter}. Body: {Body}",
                retryAfter,
                body);

            throw new AIProviderRateLimitException(
                "The itinerary AI provider rate limit was exceeded.",
                retryAfter);
        }

        if (response.StatusCode == HttpStatusCode.RequestEntityTooLarge)
        {
            throw new HttpRequestException(
                "The itinerary AI request is too large. " +
                "Reduce the requested input or output size.");
        }

        _logger.LogError(
            "Gemini itinerary API returned {StatusCode}. Body: {Body}",
            response.StatusCode,
            body);

        throw new HttpRequestException(
            $"The itinerary AI provider returned HTTP {(int)response.StatusCode}.");
    }

    private static bool IsInvalidApiKey(
        HttpStatusCode statusCode,
        string body)
    {
        if (statusCode != HttpStatusCode.BadRequest)
        {
            return false;
        }

        return body.Contains(
                   "API_KEY_INVALID",
                   StringComparison.OrdinalIgnoreCase) ||
               body.Contains(
                   "API key not valid",
                   StringComparison.OrdinalIgnoreCase);
    }

    private static TimeSpan? ResolveRetryAfter(
        HttpResponseMessage response,
        string body)
    {
        if (response.Headers.RetryAfter?.Delta is { } delta)
        {
            return delta;
        }

        if (response.Headers.RetryAfter?.Date is { } retryDate)
        {
            var remaining = retryDate - DateTimeOffset.UtcNow;
            return remaining > TimeSpan.Zero
                ? remaining
                : TimeSpan.Zero;
        }

        return TryReadGoogleRetryDelay(body);
    }

    private static TimeSpan? TryReadGoogleRetryDelay(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(body);

            if (!document.RootElement.TryGetProperty(
                    "error",
                    out var error) ||
                !error.TryGetProperty(
                    "details",
                    out var details) ||
                details.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            foreach (var detail in details.EnumerateArray())
            {
                if (!detail.TryGetProperty(
                        "retryDelay",
                        out var retryDelayElement))
                {
                    continue;
                }

                var retryDelay = retryDelayElement.GetString();
                var parsed = ParseGoogleDuration(retryDelay);
                if (parsed is not null)
                {
                    return parsed;
                }
            }
        }
        catch (JsonException)
        {
            // The original provider error is more useful than a secondary
            // parsing failure here.
        }

        return null;
    }

    private static TimeSpan? ParseGoogleDuration(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            !value.EndsWith('s'))
        {
            return null;
        }

        var number = value[..^1];
        if (!double.TryParse(
                number,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var seconds) ||
            seconds < 0)
        {
            return null;
        }

        return TimeSpan.FromSeconds(seconds);
    }

    private static string ExtractText(
        GeminiGenerateContentResponse? response)
    {
        var parts = response?.Candidates?
            .FirstOrDefault()?
            .Content?
            .Parts;

        if (parts is null || parts.Count == 0)
        {
            return string.Empty;
        }

        return string.Concat(
            parts
                .Where(part => part.Thought != true)
                .Select(part => part.Text)
                .Where(text => !string.IsNullOrWhiteSpace(text)));
    }

    private static string MapRole(string? role)
    {
        return string.Equals(
                   role,
                   "assistant",
                   StringComparison.OrdinalIgnoreCase) ||
               string.Equals(
                   role,
                   "model",
                   StringComparison.OrdinalIgnoreCase)
            ? "model"
            : "user";
    }

    private int ParseInt(string key, int fallback)
    {
        return int.TryParse(
            _configuration[$"{ConfigurationSection}:{key}"],
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var value)
            ? value
            : fallback;
    }

    private double ParseDouble(string key, double fallback)
    {
        return double.TryParse(
            _configuration[$"{ConfigurationSection}:{key}"],
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out var value)
            ? value
            : fallback;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed record GeminiSettings(
        string ApiKey,
        string BaseUrl,
        string Model,
        double Temperature,
        int MaxOutputTokens,
        double TopP,
        int ThinkingBudget,
        int TimeoutSeconds);

    private sealed class GeminiGenerateContentResponse
    {
        public List<GeminiCandidate>? Candidates { get; set; }
        public GeminiUsageMetadata? UsageMetadata { get; set; }
        public GeminiPromptFeedback? PromptFeedback { get; set; }
    }

    private sealed class GeminiCandidate
    {
        public GeminiContent? Content { get; set; }
        public string? FinishReason { get; set; }
    }

    private sealed class GeminiContent
    {
        public List<GeminiPart>? Parts { get; set; }
    }

    private sealed class GeminiPart
    {
        public string? Text { get; set; }
        public bool? Thought { get; set; }
    }

    private sealed class GeminiUsageMetadata
    {
        public int PromptTokenCount { get; set; }
        public int CandidatesTokenCount { get; set; }
        public int TotalTokenCount { get; set; }
        public int ThoughtsTokenCount { get; set; }
    }

    private sealed class GeminiPromptFeedback
    {
        public string? BlockReason { get; set; }
    }
}
