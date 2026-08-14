using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using ChatBotApi.Application.Common.DTOs;
using ChatBotApi.Application.Common.Exceptions;
using ChatBotApi.Application.Common.Interfaces;
using ChatBotApi.Application.Common.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ChatBotApi.Infrastructure.AI;

public class GroqProvider : IAIProvider
{
    private const string CompletionsUrl =
        "https://api.groq.com/openai/v1/chat/completions";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GroqProvider> _logger;
    private readonly string _configurationSectionName;

    public GroqProvider(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<GroqProvider> logger,
        string configurationSectionName)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
        _configurationSectionName =
            configurationSectionName;
    }

    public string ProviderName => _configurationSectionName;

    public string ModelName =>
        _configuration[$"{_configurationSectionName}:Model"]
        ?? "llama-3.3-70b-versatile";

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

    public async Task<string> GetChatCompletionAsync(
        string systemPrompt,
        List<ChatMessageDto> history,
        string userMessage,
        AICompletionOptions options,
        CancellationToken cancellationToken = default)
    {
        return await GetChatCompletionCoreAsync(
            systemPrompt,
            history,
            userMessage,
            options,
            cancellationToken);
    }

    private async Task<string> GetChatCompletionCoreAsync(
        string systemPrompt,
        List<ChatMessageDto> history,
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
            stream: false,
            options);

        using var client = CreateClient(
            settings.TimeoutSeconds);

        using var request = CreateRequest(
            settings.ApiKey,
            requestBody);

        var stopwatch = Stopwatch.StartNew();

        using var response = await SendAsync(
            client,
            request,
            HttpCompletionOption.ResponseContentRead,
            settings.TimeoutSeconds,
            cancellationToken);

        stopwatch.Stop();

        _logger.LogInformation(
            "Received Groq response in {ElapsedMs} ms. Status code: {StatusCode}",
            stopwatch.ElapsedMilliseconds,
            response.StatusCode);

        await EnsureSuccessAsync(
            response,
            cancellationToken);

        var responseContent =
            await response.Content.ReadAsStringAsync(
                cancellationToken);

        var result =
            JsonSerializer.Deserialize<GroqResponseJson>(
                responseContent,
                JsonOptions);

        var reply =
            result?.Choices?[0]?.Message?.Content;

        if (string.IsNullOrWhiteSpace(reply))
        {
            _logger.LogError(
                "Groq response did not contain message content. Body: {Body}",
                responseContent);

            throw new InvalidOperationException(
                "The AI provider returned an empty response.");
        }

        if (result?.Usage is not null)
        {
            _logger.LogInformation(
                "Token usage. Prompt: {PromptTokens}, Completion: {CompletionTokens}, Total: {TotalTokens}",
                result.Usage.PromptTokens,
                result.Usage.CompletionTokens,
                result.Usage.TotalTokens);
        }

        return reply;
    }

    public async IAsyncEnumerable<string>
        StreamChatCompletionAsync(
            string systemPrompt,
            List<ChatMessageDto> history,
            string userMessage,
            [EnumeratorCancellation]
            CancellationToken cancellationToken = default)
    {
        var settings = GetSettings();
        var currentHistory = history;
        var currentUserMessage = userMessage;
        var emittedResponse = new StringBuilder();

        // One automatic continuation prevents a good answer from being saved in
        // a visibly truncated state when the provider reaches its token ceiling.
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var requestBody = CreateRequestBody(
                settings,
                systemPrompt,
                currentHistory,
                currentUserMessage,
                stream: true,
                options: null);

            using var client = CreateClient(
                settings.TimeoutSeconds);

            using var request = CreateRequest(
                settings.ApiKey,
                requestBody);

            using var response = await SendAsync(
                client,
                request,
                HttpCompletionOption.ResponseHeadersRead,
                settings.TimeoutSeconds,
                cancellationToken);

            await EnsureSuccessAsync(
                response,
                cancellationToken);

            await using var responseStream =
                await response.Content.ReadAsStreamAsync(
                    cancellationToken);

            using var reader =
                new StreamReader(responseStream);

            string? finishReason = null;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var line = await reader.ReadLineAsync(
                    cancellationToken);

                if (line is null)
                {
                    break;
                }

                if (string.IsNullOrWhiteSpace(line) ||
                    !line.StartsWith(
                        "data:",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var data = line[5..].Trim();

                if (data == "[DONE]")
                {
                    break;
                }

                GroqStreamResponseJson? streamResponse;

                try
                {
                    streamResponse =
                        JsonSerializer.Deserialize
                            <GroqStreamResponseJson>(
                                data,
                                JsonOptions);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Could not parse one Groq streaming event: {EventData}",
                        data);

                    continue;
                }

                var choice = streamResponse?.Choices?[0];
                if (!string.IsNullOrWhiteSpace(choice?.FinishReason))
                {
                    finishReason = choice.FinishReason;
                }

                var content = choice?.Delta?.Content;

                if (!string.IsNullOrEmpty(content))
                {
                    emittedResponse.Append(content);
                    yield return content;
                }
            }

            var reachedLimit = string.Equals(
                    finishReason,
                    "length",
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    finishReason,
                    "max_tokens",
                    StringComparison.OrdinalIgnoreCase);

            if (!reachedLimit || attempt == 1)
            {
                if (reachedLimit)
                {
                    _logger.LogWarning(
                        "Groq response still reached the token limit after automatic continuation.");
                }

                yield break;
            }

            _logger.LogInformation(
                "Groq response reached the token limit; continuing it once automatically.");

            currentHistory = new List<ChatMessageDto>(history)
            {
                new() { Role = "user", Content = userMessage },
                new() { Role = "assistant", Content = emittedResponse.ToString() }
            };
            currentUserMessage =
                "Continue exactly where the previous answer stopped. Do not repeat earlier content. Finish all remaining sections concisely and end with a complete sentence.";
        }
    }

    private GroqSettings GetSettings()
    {
        var sectionPrefix =
            $"{_configurationSectionName}:";

        var apiKey =
            _configuration[
                $"{sectionPrefix}ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogError(
                "Groq API key is not configured for {ConfigurationSection}.",
                _configurationSectionName);

            throw new InvalidOperationException(
                $"Groq API key is not configured for {_configurationSectionName}.");
        }

        return new GroqSettings(
            apiKey,
            _configuration[
                $"{sectionPrefix}Model"]
                ?? "llama-3.3-70b-versatile",

            double.TryParse(
                _configuration[
                    $"{sectionPrefix}Temperature"],
                out var temperature)
                    ? temperature
                    : 0.7,

            int.TryParse(
                _configuration[
                    $"{sectionPrefix}MaxTokens"],
                out var maxTokens)
                    ? maxTokens
                    : 1200,

            double.TryParse(
                _configuration[
                    $"{sectionPrefix}TopP"],
                out var topP)
                    ? topP
                    : 0.9,

            int.TryParse(
                _configuration[
                    $"{sectionPrefix}TimeoutSeconds"],
                out var timeoutSeconds)
                    ? timeoutSeconds
                    : 30);
    }

    private static object CreateRequestBody(
        GroqSettings settings,
        string systemPrompt,
        IEnumerable<ChatMessageDto> history,
        string userMessage,
        bool stream,
        AICompletionOptions? options)
    {
        var messages = new List<object>
        {
            new
            {
                role = "system",
                content = systemPrompt
            }
        };

        messages.AddRange(
            history.Select(message => new
            {
                role = message.Role,
                content = message.Content
            }));

        messages.Add(new
        {
            role = "user",
            content = userMessage
        });

        var requestBody = new Dictionary<string, object?>
        {
            ["model"] = settings.Model,
            ["messages"] = messages,
            ["temperature"] =
                options?.Temperature ?? settings.Temperature,
            ["max_tokens"] =
                options?.MaxTokens is > 0
                    ? Math.Min(
                        options.MaxTokens.Value,
                        settings.MaxTokens)
                    : settings.MaxTokens,
            ["top_p"] =
                options?.TopP ?? settings.TopP,
            ["stream"] = stream
        };

        if (!string.IsNullOrWhiteSpace(
                options?.ReasoningEffort))
        {
            requestBody["reasoning_effort"] =
                options.ReasoningEffort;
        }

        return requestBody;
    }

    private HttpClient CreateClient(
        int timeoutSeconds)
    {
        var client =
            _httpClientFactory.CreateClient();

        client.Timeout =
            TimeSpan.FromSeconds(
                timeoutSeconds);

        return client;
    }

    private static HttpRequestMessage CreateRequest(
        string apiKey,
        object requestBody)
    {
        var request =
            new HttpRequestMessage(
                HttpMethod.Post,
                CompletionsUrl);

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                apiKey);

        request.Content =
            new StringContent(
                JsonSerializer.Serialize(
                    requestBody),
                Encoding.UTF8,
                "application/json");

        return request;
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpClient client,
        HttpRequestMessage request,
        HttpCompletionOption completionOption,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        try
        {
            return await client.SendAsync(
                request,
                completionOption,
                cancellationToken);
        }
        catch (TaskCanceledException ex)
            when (!cancellationToken
                .IsCancellationRequested)
        {
            _logger.LogError(
                ex,
                "Groq request timed out after {TimeoutSeconds} seconds.",
                timeoutSeconds);

            throw new TimeoutException(
                "The AI provider request timed out.",
                ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "Could not connect to the Groq API.");

            throw new HttpRequestException(
                "Could not connect to the AI provider.",
                ex);
        }
    }

    private async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body =
            await response.Content.ReadAsStringAsync(
                cancellationToken);

        if (response.StatusCode is
            HttpStatusCode.Unauthorized or
            HttpStatusCode.Forbidden)
        {
            _logger.LogError(
                "Groq rejected the API key. Status: {StatusCode}",
                response.StatusCode);

            throw new UnauthorizedAccessException(
                "The AI provider API key is invalid or unauthorized.");
        }

        if (response.StatusCode ==
            HttpStatusCode.RequestEntityTooLarge)
        {
            _logger.LogWarning(
                "Groq rejected an oversized request. Body: {Body}",
                body);

            throw new HttpRequestException(
                "The AI request is too large. Reduce the requested input or output size.");
        }

        if (response.StatusCode ==
            HttpStatusCode.TooManyRequests)
        {
            _logger.LogWarning(
                "Groq rate limit was exceeded. Body: {Body}",
                body);

            throw new AIProviderRateLimitException(
                "The AI provider rate limit was exceeded.",
                GetRetryAfter(response, body));
        }

        _logger.LogError(
            "Groq returned {StatusCode}. Body: {Body}",
            response.StatusCode,
            body);

        throw new HttpRequestException(
            "The AI provider returned an error.");
    }

    internal static TimeSpan? GetRetryAfter(
        HttpResponseMessage response,
        string? responseBody)
    {
        var retryAfter = response.Headers.RetryAfter;

        if (retryAfter?.Delta is { } delta)
        {
            return delta;
        }

        if (retryAfter?.Date is { } retryDate)
        {
            var remaining =
                retryDate - DateTimeOffset.UtcNow;

            return remaining > TimeSpan.Zero
                ? remaining
                : TimeSpan.Zero;
        }

        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return null;
        }

        var match = Regex.Match(
            responseBody,
            @"(?:try\s+again\s+in|retry\s+after)\s*(?<seconds>\d+(?:\.\d+)?)\s*s",
            RegexOptions.IgnoreCase |
            RegexOptions.CultureInvariant);

        if (!match.Success ||
            !double.TryParse(
                match.Groups["seconds"].Value,
                NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out var seconds) ||
            seconds < 0)
        {
            return null;
        }

        return TimeSpan.FromSeconds(seconds);
    }

    private static readonly JsonSerializerOptions
        JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

    private sealed record GroqSettings(
        string ApiKey,
        string Model,
        double Temperature,
        int MaxTokens,
        double TopP,
        int TimeoutSeconds);

    private sealed class GroqResponseJson
    {
        public List<GroqChoiceJson>? Choices
        {
            get;
            set;
        }

        public GroqUsageJson? Usage
        {
            get;
            set;
        }
    }

    private sealed class GroqChoiceJson
    {
        public GroqMessageJson? Message
        {
            get;
            set;
        }
    }

    private sealed class GroqMessageJson
    {
        public string? Content
        {
            get;
            set;
        }
    }

    private sealed class GroqStreamResponseJson
    {
        public List<GroqStreamChoiceJson>? Choices
        {
            get;
            set;
        }
    }

    private sealed class GroqStreamChoiceJson
    {
        public GroqDeltaJson? Delta
        {
            get;
            set;
        }

        [JsonPropertyName("finish_reason")]
        public string? FinishReason
        {
            get;
            set;
        }
    }

    private sealed class GroqDeltaJson
    {
        public string? Content
        {
            get;
            set;
        }
    }

    private sealed class GroqUsageJson
    {
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens
        {
            get;
            set;
        }

        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens
        {
            get;
            set;
        }

        [JsonPropertyName("total_tokens")]
        public int TotalTokens
        {
            get;
            set;
        }
    }
}
