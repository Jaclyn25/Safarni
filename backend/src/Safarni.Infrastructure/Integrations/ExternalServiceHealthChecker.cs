using System.Diagnostics;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ChatBotApi.Application.Common.Interfaces;
using Safarni.Application.Abstractions.Integrations;
using Safarni.Infrastructure.Email;
using Safarni.Infrastructure.Persistence;

namespace Safarni.Infrastructure.Integrations;

public sealed class ExternalServiceHealthChecker : IExternalServiceHealthChecker
{
    private const string GroqModelsUrl = "https://api.groq.com/openai/v1/models";
    private const string PhotoDirectoryRelativePath = "wwwroot/uploads/profile-photos";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly IQdrantService _qdrantService;
    private readonly ApplicationDbContext _dbContext;
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<ExternalServiceHealthChecker> _logger;

    public ExternalServiceHealthChecker(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IQdrantService qdrantService,
        ApplicationDbContext dbContext,
        IOptions<EmailSettings> emailSettings,
        ILogger<ExternalServiceHealthChecker> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _qdrantService = qdrantService;
        _dbContext = dbContext;
        _emailSettings = emailSettings.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<IntegrationHealthCheck>> CheckAllAsync(
        CancellationToken cancellationToken = default)
    {
        var checks = new List<IntegrationHealthCheck>
        {
            await CheckGroqAsync("groq-chat", "Groq Chat", "GroqChat", "AI chat assistant", cancellationToken),
            CheckOpenRouterPresence(),
            await CheckGroqAsync("groq-itinerary", "Groq Itinerary", "GroqItinerary", "Itinerary generation", cancellationToken),
            await CheckGeminiAsync(cancellationToken),
            CheckPresence("google-places", "Google Places", "GooglePlaces:ApiKey", "Chat place search"),
            CheckPresence("google-routes", "Google Routes", "GoogleRoutes:ApiKey", "Trip route times"),
            CheckSerpApiPresence(
                "serpapi-discovery",
                "SerpAPI Discovery",
                "SerpApi:DiscoveryApiKey",
                "Attractions, restaurants and itinerary images"),
            CheckSerpApiPresence(
                "serpapi-commerce",
                "SerpAPI Commerce",
                "SerpApi:CommerceApiKey",
                "Flights, airports and hotels"),
            await CheckQdrantAsync(cancellationToken),
            CheckEmail(),
            CheckProfilePhotoStorage(),
            await CheckDatabaseAsync(cancellationToken)
        };

        return checks;
    }

    public async Task<IntegrationHealthCheck?> CheckByNameAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        var checks = await CheckAllAsync(cancellationToken);
        return checks.FirstOrDefault(check =>
            string.Equals(check.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<IntegrationConfigEntry> GetConfigStatus()
    {
        var entries = new List<IntegrationConfigEntry>
        {
            Entry("groq-chat", "GroqChat:ApiKey"),
            Entry("groq-itinerary", "GroqItinerary:ApiKey"),
            Entry("gemini-itinerary", "GeminiItinerary:ApiKey"),
            Entry("google-places", "GooglePlaces:ApiKey"),
            Entry("google-routes", "GoogleRoutes:ApiKey"),
            SerpApiEntry(
                "serpapi-discovery",
                "SerpApi:DiscoveryApiKey"),
            SerpApiEntry(
                "serpapi-commerce",
                "SerpApi:CommerceApiKey"),
            Entry("email", "EmailSettings:UserName"),
            Entry("email", "EmailSettings:Password")
        };

        return entries;
    }

    private IntegrationConfigEntry Entry(string integration, string configKey)
    {
        var value = _configuration[configKey];
        var configured = IsRealValue(value);

        return new IntegrationConfigEntry(
            integration,
            configKey,
            configured,
            configured ? MaskValue(value!) : null);
    }

    private IntegrationConfigEntry SerpApiEntry(
        string integration,
        string configKey)
    {
        var environmentKey = string.Equals(
            configKey,
            "SerpApi:DiscoveryApiKey",
            StringComparison.OrdinalIgnoreCase)
                ? "SERPAPI_DISCOVERY_API_KEY"
                : "SERPAPI_COMMERCE_API_KEY";
        var value = Environment.GetEnvironmentVariable(environmentKey) ??
                    _configuration[configKey] ??
                    _configuration["SerpApi:ApiKey"];
        var configured = IsRealValue(value);

        return new IntegrationConfigEntry(
            integration,
            configKey,
            configured,
            configured ? MaskValue(value!) : null);
    }

    private IntegrationHealthCheck CheckSerpApiPresence(
        string name,
        string displayName,
        string configKey,
        string affectedFeature)
    {
        var environmentKey = string.Equals(
            configKey,
            "SerpApi:DiscoveryApiKey",
            StringComparison.OrdinalIgnoreCase)
                ? "SERPAPI_DISCOVERY_API_KEY"
                : "SERPAPI_COMMERCE_API_KEY";
        var value = Environment.GetEnvironmentVariable(environmentKey) ??
                    _configuration[configKey] ??
                    _configuration["SerpApi:ApiKey"];

        return IsRealValue(value)
            ? Healthy(
                name,
                displayName,
                affectedFeature,
                "API key is configured (no live call made).",
                0)
            : NotConfigured(
                name,
                displayName,
                affectedFeature,
                "API key is not configured.");
    }

    private IntegrationHealthCheck CheckOpenRouterPresence()
    {
        var value = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY") ??
                    _configuration["OpenRouter:ApiKey"];

        return IsRealValue(value)
            ? Healthy(
                "openrouter-embeddings",
                "OpenRouter Embeddings",
                "RAG semantic retrieval",
                "Embedding API key is configured.",
                0)
            : NotConfigured(
                "openrouter-embeddings",
                "OpenRouter Embeddings",
                "RAG semantic retrieval",
                "OPENROUTER_API_KEY is not configured.");
    }

    private async Task<IntegrationHealthCheck> CheckGroqAsync(
        string name,
        string displayName,
        string section,
        string affectedFeature,
        CancellationToken cancellationToken)
    {
        var apiKey = _configuration[$"{section}:ApiKey"];
        if (!IsRealValue(apiKey))
        {
            return NotConfigured(name, displayName, affectedFeature, "API key is not configured.");
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            using var request = new HttpRequestMessage(HttpMethod.Get, GroqModelsUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            using var response = await client.SendAsync(request, cancellationToken);

            stopwatch.Stop();

            if (response.IsSuccessStatusCode)
            {
                return Healthy(name, displayName, affectedFeature, "API key accepted.", stopwatch.ElapsedMilliseconds);
            }

            _logger.LogWarning(
                "Health check for {Integration} returned {StatusCode}.",
                name,
                response.StatusCode);

            return new IntegrationHealthCheck(
                name, displayName, affectedFeature,
                StatusUnhealthy, $"API returned HTTP {(int)response.StatusCode}.",
                true, DateTime.UtcNow, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex, "Health check for {Integration} failed.", name);
            return new IntegrationHealthCheck(
                name, displayName, affectedFeature,
                StatusUnhealthy, $"Not reachable: {ex.GetType().Name}",
                true, DateTime.UtcNow, stopwatch.ElapsedMilliseconds);
        }
    }

    private async Task<IntegrationHealthCheck> CheckGeminiAsync(
        CancellationToken cancellationToken)
    {
        const string name = "gemini-itinerary";
        const string displayName = "Gemini Itinerary";
        const string affectedFeature = "Itinerary generation";

        var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ??
            Environment.GetEnvironmentVariable("GEMINI_ITINERARY_API_KEY") ??
            _configuration["GeminiItinerary:ApiKey"];

        if (!IsRealValue(apiKey))
        {
            return NotConfigured(name, displayName, affectedFeature, "API key is not configured.");
        }

        var baseUrl = (_configuration["GeminiItinerary:BaseUrl"] ??
            "https://generativelanguage.googleapis.com/v1beta").TrimEnd('/');

        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            using var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/models");
            request.Headers.TryAddWithoutValidation("x-goog-api-key", apiKey);

            using var response = await client.SendAsync(request, cancellationToken);

            stopwatch.Stop();

            if (response.IsSuccessStatusCode)
            {
                return Healthy(name, displayName, affectedFeature, "API key accepted.", stopwatch.ElapsedMilliseconds);
            }

            return new IntegrationHealthCheck(
                name, displayName, affectedFeature,
                StatusUnhealthy, $"API returned HTTP {(int)response.StatusCode}.",
                true, DateTime.UtcNow, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex, "Health check for Gemini itinerary failed.");
            return new IntegrationHealthCheck(
                name, displayName, affectedFeature,
                StatusUnhealthy, $"Not reachable: {ex.GetType().Name}",
                true, DateTime.UtcNow, stopwatch.ElapsedMilliseconds);
        }
    }

    private async Task<IntegrationHealthCheck> CheckQdrantAsync(
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        if (!IsRealValue(_configuration["RAG:QdrantUrl"]))
        {
            stopwatch.Stop();
            return NotConfigured("qdrant", "Qdrant", "RAG knowledge search", "Qdrant URL is not configured.");
        }

        bool healthy;
        try
        {
            healthy = await _qdrantService.IsHealthyAsync();
        }
        catch (Exception ex)
        {
            healthy = false;
            _logger.LogWarning(ex, "Qdrant health check failed.");
        }

        stopwatch.Stop();

        return healthy
            ? Healthy("qdrant", "Qdrant", "RAG knowledge search", "Vector store is reachable.", stopwatch.ElapsedMilliseconds)
            : new IntegrationHealthCheck(
                "qdrant", "Qdrant", "RAG knowledge search",
                StatusUnhealthy, "Vector store is not reachable.",
                true, DateTime.UtcNow, stopwatch.ElapsedMilliseconds);
    }

    private IntegrationHealthCheck CheckEmail()
    {
        const string name = "email";
        const string displayName = "Email (SMTP)";
        const string affectedFeature = "Password reset and verification emails";

        if (!IsRealValue(_emailSettings.Host) ||
            _emailSettings.Port <= 0 ||
            !IsRealValue(_emailSettings.From) ||
            !IsRealValue(_emailSettings.UserName) ||
            !IsRealValue(_emailSettings.Password))
        {
            return NotConfigured(name, displayName, affectedFeature, "SMTP credentials are not configured.");
        }

        return Healthy(name, displayName, affectedFeature, "SMTP settings are configured.", 0);
    }

    private IntegrationHealthCheck CheckProfilePhotoStorage()
    {
        const string name = "profile-photo-storage";
        const string displayName = "Profile photo storage";
        const string affectedFeature = "Profile photos";

        try
        {
            var folderPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                PhotoDirectoryRelativePath);

            Directory.CreateDirectory(folderPath);

            var probe = Path.Combine(folderPath, $".probe-{Guid.NewGuid():N}.tmp");
            File.WriteAllText(probe, "ok");
            File.Delete(probe);

            return Healthy(name, displayName, affectedFeature, "Local storage directory is writable.", 0);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Profile photo storage health check failed.");
            return new IntegrationHealthCheck(
                name, displayName, affectedFeature,
                StatusUnhealthy, $"Storage is not writable: {ex.GetType().Name}",
                true, DateTime.UtcNow, 0);
        }
    }

    private async Task<IntegrationHealthCheck> CheckDatabaseAsync(
        CancellationToken cancellationToken)
    {
        const string name = "database";
        const string displayName = "SQL Server";
        const string affectedFeature = "Core data storage";

        var stopwatch = Stopwatch.StartNew();
        bool healthy;
        try
        {
            healthy = await _dbContext.Database.CanConnectAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            healthy = false;
            _logger.LogWarning(ex, "Database health check failed.");
        }

        stopwatch.Stop();

        return healthy
            ? Healthy(name, displayName, affectedFeature, "Database is reachable.", stopwatch.ElapsedMilliseconds)
            : new IntegrationHealthCheck(
                name, displayName, affectedFeature,
                StatusUnhealthy, "Database is not reachable.",
                true, DateTime.UtcNow, stopwatch.ElapsedMilliseconds);
    }

    private IntegrationHealthCheck CheckPresence(
        string name,
        string displayName,
        string configKey,
        string affectedFeature)
    {
        var value = _configuration[configKey];
        if (!IsRealValue(value))
        {
            return NotConfigured(name, displayName, affectedFeature, "API key is not configured.");
        }

        return Healthy(name, displayName, affectedFeature, "API key is configured (no live call made).", 0);
    }

    private static IntegrationHealthCheck NotConfigured(
        string name,
        string displayName,
        string affectedFeature,
        string detail)
    {
        return new IntegrationHealthCheck(
            name, displayName, affectedFeature,
            StatusNotConfigured, detail,
            false, DateTime.UtcNow, 0);
    }

    private static IntegrationHealthCheck Healthy(
        string name,
        string displayName,
        string affectedFeature,
        string detail,
        long latencyMs)
    {
        return new IntegrationHealthCheck(
            name, displayName, affectedFeature,
            StatusHealthy, detail,
            true, DateTime.UtcNow, latencyMs);
    }

    private static bool IsRealValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return !value.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(value, "CHANGE_ME", StringComparison.OrdinalIgnoreCase);
    }

    private static string MaskValue(string value)
    {
        if (value.Length <= 8)
        {
            return "••••";
        }

        return $"{value[..2]}••••{value[^4..]}";
    }

    public const string StatusHealthy = "Healthy";
    public const string StatusUnhealthy = "Unhealthy";
    public const string StatusNotConfigured = "NotConfigured";
}
