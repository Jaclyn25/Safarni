using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using ChatBotApi.Application.Common.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Safarni.Application.Abstractions.AI;

namespace ChatBotApi.Infrastructure.AI;

/// <summary>
/// Gemini function-calling client used by Safarni's end-to-end itinerary agent.
/// The application layer owns the loop and executes every approved tool.
/// </summary>
public sealed class GeminiItineraryAgentModel : IItineraryAgentModel
{
    private const string ConfigurationSection = "GeminiItinerary";
    private const string DefaultBaseUrl =
        "https://generativelanguage.googleapis.com/v1beta";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GeminiItineraryAgentModel> _logger;

    public GeminiItineraryAgentModel(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<GeminiItineraryAgentModel> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public string ProviderName => "GeminiItineraryAgent";

    public string ModelName =>
        _configuration[$"{ConfigurationSection}:Model"] ??
        "gemini-3.6-flash";

    public int MaxAgentSteps => Math.Clamp(
        ParseInt("AgentMaxSteps", 8),
        4,
        10);

    public async Task<ItineraryAgentModelResponse> GetNextDecisionAsync(
        string systemPrompt,
        IReadOnlyList<ItineraryAgentMessage> messages,
        IReadOnlyList<ItineraryAgentToolDefinition> tools,
        CancellationToken cancellationToken = default)
    {
        var settings = GetSettings();
        var requestBody = CreateRequestBody(
            settings,
            systemPrompt,
            messages,
            tools);

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

        using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseContentRead,
            cancellationToken);
        var body = await response.Content.ReadAsStringAsync(
            cancellationToken);

        EnsureSuccess(response, body);

        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;

            ItineraryAgentTokenUsage? tokenUsage = null;

            if (root.TryGetProperty("usageMetadata", out var usage))
            {
                tokenUsage = new ItineraryAgentTokenUsage(
                    ReadLong(usage, "promptTokenCount"),
                    ReadLong(usage, "candidatesTokenCount"),
                    ReadLong(usage, "thoughtsTokenCount"),
                    ReadLong(usage, "toolUsePromptTokenCount"),
                    ReadLong(usage, "cachedContentTokenCount"),
                    ReadLong(usage, "totalTokenCount"));

                _logger.LogInformation(
                    "Gemini itinerary agent actual token usage. Prompt={PromptTokens}, Output={OutputTokens}, Thinking={ThinkingTokens}, ToolUsePrompt={ToolUsePromptTokens}, Cached={CachedTokens}, Total={TotalTokens}",
                    tokenUsage.PromptTokenCount,
                    tokenUsage.CandidatesTokenCount,
                    tokenUsage.ThoughtsTokenCount,
                    tokenUsage.ToolUsePromptTokenCount,
                    tokenUsage.CachedContentTokenCount,
                    tokenUsage.TotalTokenCount);
            }

            if (!root.TryGetProperty("candidates", out var candidates) ||
                candidates.ValueKind != JsonValueKind.Array ||
                candidates.GetArrayLength() == 0)
            {
                throw new InvalidOperationException(
                    "The itinerary agent model returned no candidate.");
            }

            var candidate = candidates[0];
            var finishReason = candidate.TryGetProperty(
                "finishReason",
                out var finishReasonElement)
                ? finishReasonElement.GetString()
                : null;

            if (string.Equals(
                    finishReason,
                    "MAX_TOKENS",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "The itinerary agent decision reached its output token limit.");
            }

            if (!candidate.TryGetProperty("content", out var content) ||
                !content.TryGetProperty("parts", out var parts) ||
                parts.ValueKind != JsonValueKind.Array)
            {
                return new ItineraryAgentModelResponse(
                    null,
                    [],
                    null,
                    tokenUsage);
            }

            var text = new StringBuilder();
            var calls = new List<ItineraryAgentFunctionCall>();

            foreach (var part in parts.EnumerateArray())
            {
                if (part.TryGetProperty("thought", out var thought) &&
                    thought.ValueKind == JsonValueKind.True)
                {
                    continue;
                }

                if (part.TryGetProperty("text", out var textElement) &&
                    textElement.ValueKind == JsonValueKind.String)
                {
                    text.Append(textElement.GetString());
                }

                if (!part.TryGetProperty(
                        "functionCall",
                        out var functionCall) ||
                    functionCall.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var name = functionCall.TryGetProperty(
                    "name",
                    out var nameElement)
                    ? nameElement.GetString()
                    : null;
                var id = functionCall.TryGetProperty(
                    "id",
                    out var idElement)
                    ? idElement.GetString()
                    : null;

                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                var argumentsJson = functionCall.TryGetProperty(
                    "args",
                    out var arguments)
                    ? arguments.GetRawText()
                    : "{}";
                var thoughtSignature = part.TryGetProperty(
                    "thoughtSignature",
                    out var signatureElement)
                    ? signatureElement.GetString()
                    : null;

                calls.Add(new ItineraryAgentFunctionCall(
                    string.IsNullOrWhiteSpace(id)
                        ? $"call_{Guid.NewGuid():N}"
                        : id,
                    name,
                    argumentsJson,
                    thoughtSignature));
            }

            return new ItineraryAgentModelResponse(
                text.Length == 0 ? null : text.ToString().Trim(),
                calls,
                content.GetRawText(),
                tokenUsage);
        }
        catch (JsonException exception)
        {
            _logger.LogError(
                exception,
                "Gemini itinerary agent returned malformed JSON. Body: {Body}",
                body);
            throw new InvalidOperationException(
                "The itinerary agent provider returned an invalid response.",
                exception);
        }
    }

    private object CreateRequestBody(
        GeminiAgentSettings settings,
        string systemPrompt,
        IReadOnlyList<ItineraryAgentMessage> messages,
        IReadOnlyList<ItineraryAgentToolDefinition> tools)
    {
        var contents = messages
            .Select(CreateContent)
            .ToList();
        var functionDeclarations = tools
            .Select(tool => new Dictionary<string, object?>
            {
                ["name"] = tool.Name,
                ["description"] = tool.Description,
                ["parameters"] = ParseJson(tool.ParametersJson)
            })
            .ToArray();

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
            ["tools"] = new object[]
            {
                new Dictionary<string, object?>
                {
                    ["functionDeclarations"] = functionDeclarations
                }
            },
            ["toolConfig"] = new
            {
                functionCallingConfig = new
                {
                    mode = "AUTO"
                }
            },
            ["generationConfig"] = new Dictionary<string, object?>
            {
                ["temperature"] = 0.1,
                ["maxOutputTokens"] = settings.AgentDecisionMaxOutputTokens,
                ["thinkingConfig"] = new
                {
                    thinkingLevel = "low"
                }
            }
        };
    }

    private static object CreateContent(
        ItineraryAgentMessage message)
    {
        if (message.Role == ItineraryAgentMessageRole.Model &&
            !string.IsNullOrWhiteSpace(message.RawContentJson))
        {
            // Gemini 3 requires function-call thought signatures and call IDs
            // to be replayed exactly. Preserve the original model content.
            return ParseJson(message.RawContentJson);
        }

        var parts = new List<object>();

        if (!string.IsNullOrWhiteSpace(message.Text))
        {
            parts.Add(new { text = message.Text });
        }

        if (message.FunctionCalls is { Count: > 0 })
        {
            foreach (var call in message.FunctionCalls)
            {
                var part = new Dictionary<string, object?>
                {
                    ["functionCall"] = new Dictionary<string, object?>
                    {
                        ["id"] = call.Id,
                        ["name"] = call.Name,
                        ["args"] = ParseJson(call.ArgumentsJson)
                    }
                };

                if (!string.IsNullOrWhiteSpace(call.ThoughtSignature))
                {
                    part["thoughtSignature"] = call.ThoughtSignature;
                }

                parts.Add(part);
            }
        }

        if (message.FunctionResults is { Count: > 0 })
        {
            foreach (var result in message.FunctionResults)
            {
                parts.Add(new Dictionary<string, object?>
                {
                    ["functionResponse"] = new Dictionary<string, object?>
                    {
                        ["id"] = result.Id,
                        ["name"] = result.Name,
                        ["response"] = ParseJson(result.ResultJson)
                    }
                });
            }
        }

        return new Dictionary<string, object?>
        {
            ["role"] = message.Role == ItineraryAgentMessageRole.Model
                ? "model"
                : "user",
            ["parts"] = parts
        };
    }

    private GeminiAgentSettings GetSettings()
    {
        var apiKey =
            Environment.GetEnvironmentVariable("GEMINI_API_KEY") ??
            Environment.GetEnvironmentVariable("GEMINI_ITINERARY_API_KEY") ??
            _configuration[$"{ConfigurationSection}:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "Gemini itinerary API key is not configured. Set GEMINI_ITINERARY_API_KEY (or GEMINI_API_KEY) in your environment.");
        }

        return new GeminiAgentSettings(
            apiKey.Trim(),
            (_configuration[$"{ConfigurationSection}:BaseUrl"] ??
             DefaultBaseUrl).TrimEnd('/'),
            _configuration[$"{ConfigurationSection}:Model"] ??
                "gemini-3.6-flash",
            Math.Clamp(
                ParseInt("AgentDecisionMaxOutputTokens", 6000),
                1024,
                12000),
            ParseInt("TimeoutSeconds", 120));
    }

    private void EnsureSuccess(
        HttpResponseMessage response,
        string body)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            var retryAfter = response.Headers.RetryAfter?.Delta;
            throw new AIProviderRateLimitException(
                "The itinerary agent provider rate limit was exceeded.",
                retryAfter);
        }

        if (response.StatusCode is HttpStatusCode.Unauthorized or
            HttpStatusCode.Forbidden ||
            body.Contains(
                "API_KEY_INVALID",
                StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "Gemini itinerary agent authentication failed with HTTP {StatusCode}. Provider message: {ProviderMessage}",
                (int)response.StatusCode,
                ExtractProviderMessage(body) ?? "No provider message was returned.");

            throw new UnauthorizedAccessException(
                "The itinerary agent provider API key is invalid or unauthorized.");
        }

        _logger.LogError(
            "Gemini itinerary agent returned {StatusCode}. Body: {Body}",
            response.StatusCode,
            body);

        throw new HttpRequestException(
            $"The itinerary agent provider returned HTTP {(int)response.StatusCode}.");
    }

    private static string? ExtractProviderMessage(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            if (!document.RootElement.TryGetProperty("error", out var error) ||
                error.ValueKind != JsonValueKind.Object ||
                !error.TryGetProperty("message", out var message) ||
                message.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            return message.GetString();
        }
        catch (JsonException)
        {
            return null;
        }
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

    private static JsonElement ParseJson(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(
                string.IsNullOrWhiteSpace(json) ? "{}" : json);
            return document.RootElement.Clone();
        }
        catch (JsonException)
        {
            using var fallback = JsonDocument.Parse("{}");
            return fallback.RootElement.Clone();
        }
    }

    private static long ReadLong(
        JsonElement parent,
        string propertyName)
    {
        return parent.TryGetProperty(propertyName, out var value) &&
               value.TryGetInt64(out var result)
            ? result
            : 0L;
    }

    private sealed record GeminiAgentSettings(
        string ApiKey,
        string BaseUrl,
        string Model,
        int AgentDecisionMaxOutputTokens,
        int TimeoutSeconds);
}
