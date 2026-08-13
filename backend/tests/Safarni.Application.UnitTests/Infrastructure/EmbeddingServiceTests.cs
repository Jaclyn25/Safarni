using System.Net;
using System.Text.Json;
using ChatBotApi.Application.Common.Interfaces;
using ChatBotApi.Infrastructure.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace Safarni.Application.UnitTests.Infrastructure;

public class EmbeddingServiceTests
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<IAIProvider> _aiProviderMock;
    private readonly Mock<ILogger<EmbeddingService>> _loggerMock;
    private readonly HttpClient _httpClient;

    public EmbeddingServiceTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(() => 
        {
            return new HttpClient(_httpMessageHandlerMock.Object, disposeHandler: false)
            {
                BaseAddress = new Uri("https://openrouter.ai/")
            };
        });

        _configurationMock = new Mock<IConfiguration>();
        _configurationMock.Setup(c => c["OpenRouter:ApiKey"]).Returns("test-api-key");
        _configurationMock.Setup(c => c["OpenRouter:BaseUrl"]).Returns("https://openrouter.ai/api/v1");
        _configurationMock.Setup(c => c["OpenRouter:EmbeddingModel"]).Returns("nvidia/nemotron-3-embed-1b:free");

        _aiProviderMock = new Mock<IAIProvider>();
        _loggerMock = new Mock<ILogger<EmbeddingService>>();
        
        Environment.SetEnvironmentVariable("OPENROUTER_API_KEY", "test-api-key");
    }

    private EmbeddingService CreateService()
    {
        return new EmbeddingService(
            _httpClientFactoryMock.Object,
            _configurationMock.Object,
            _aiProviderMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_ValidText_ReturnsEmbedding()
    {
        // Arrange
        var expectedEmbedding = new float[] { 0.1f, 0.2f, 0.3f };
        var responsePayload = new
        {
            data = new[]
            {
                new { index = 0, embedding = expectedEmbedding }
            }
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(responsePayload))
            });

        var service = CreateService();

        // Act
        var result = await service.GenerateEmbeddingAsync("Test text");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedEmbedding.Length, result.Length);
        Assert.Equal(expectedEmbedding, result);
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_EmptyText_ThrowsArgumentException()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.GenerateEmbeddingAsync(""));
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Unauthorized,
                Content = new StringContent("Unauthorized")
            });

        var service = CreateService();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.GenerateEmbeddingAsync("Test"));
        Assert.IsType<UnauthorizedAccessException>(exception.InnerException);
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_TooManyRequests_ThrowsHttpRequestExceptionAfterRetries()
    {
        // Arrange
        int callCount = 0;
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() => 
            {
                callCount++;
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.TooManyRequests,
                    Content = new StringContent("Rate Limit Exceeded")
                };
            });

        var service = CreateService();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.GenerateEmbeddingAsync("Test"));
        Assert.IsType<HttpRequestException>(exception.InnerException);
        Assert.Contains("rate limit", exception.InnerException.Message);
        
        // Initial request + 3 retries = 4 calls total
        Assert.Equal(4, callCount);
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_429Then200_SucceedsAfterOneRetry()
    {
        var expectedEmbedding = new float[] { 0.1f, 0.2f, 0.3f };
        var responsePayload = new { data = new[] { new { index = 0, embedding = expectedEmbedding } } };
        
        int callCount = 0;
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage request, CancellationToken token) =>
            {
                callCount++;
                if (callCount == 1)
                {
                    return new HttpResponseMessage(HttpStatusCode.TooManyRequests) { Content = new StringContent("Rate Limit Exceeded") };
                }
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(responsePayload)) };
            });

        var service = CreateService();
        var result = await service.GenerateEmbeddingAsync("Test");

        Assert.Equal(2, callCount);
        Assert.Equal(expectedEmbedding, result);
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_500Then200_SucceedsAfterRetry()
    {
        var expectedEmbedding = new float[] { 0.1f, 0.2f, 0.3f };
        var responsePayload = new { data = new[] { new { index = 0, embedding = expectedEmbedding } } };
        
        int callCount = 0;
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage request, CancellationToken token) =>
            {
                callCount++;
                if (callCount == 1)
                {
                    return new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent("Server Error") };
                }
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(responsePayload)) };
            });

        var service = CreateService();
        var result = await service.GenerateEmbeddingAsync("Test");

        Assert.Equal(2, callCount);
        Assert.Equal(expectedEmbedding, result);
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_BadRequest_ThrowsImmediatelyWithoutRetry()
    {
        int callCount = 0;
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage request, CancellationToken token) =>
            {
                callCount++;
                return new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("Bad Request") };
            });

        var service = CreateService();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.GenerateEmbeddingAsync("Test"));
        Assert.IsType<HttpRequestException>(exception.InnerException);
        Assert.Equal(1, callCount); // Fails immediately, no retry
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_RespectsRetryAfterHeader()
    {
        var expectedEmbedding = new float[] { 0.1f, 0.2f, 0.3f };
        var responsePayload = new { data = new[] { new { index = 0, embedding = expectedEmbedding } } };
        
        int callCount = 0;
        var start = DateTime.UtcNow;

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage request, CancellationToken token) =>
            {
                callCount++;
                if (callCount == 1)
                {
                    var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests) { Content = new StringContent("Rate Limit Exceeded") };
                    response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(1));
                    return response;
                }
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(responsePayload)) };
            });

        var service = CreateService();
        var result = await service.GenerateEmbeddingAsync("Test");
        var duration = DateTime.UtcNow - start;

        Assert.Equal(2, callCount);
        Assert.True(duration.TotalSeconds >= 1);
        Assert.Equal(expectedEmbedding, result);
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_MissingApiKey_ThrowsInvalidOperationException()
    {
        // Arrange
        Environment.SetEnvironmentVariable("OPENROUTER_API_KEY", null);
        _configurationMock.Setup(c => c["OpenRouter:ApiKey"]).Returns((string)null);

        var service = CreateService();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.GenerateEmbeddingAsync("Test text"));
        Assert.Contains("OpenRouter API key is not configured", exception.InnerException?.Message ?? exception.Message);
        
        // Restore for other tests
        Environment.SetEnvironmentVariable("OPENROUTER_API_KEY", "test-api-key");
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_CorrectModelPayloadSent()
    {
        // Arrange
        HttpRequestMessage capturedRequest = null;
        var expectedEmbedding = new float[] { 0.1f, 0.2f, 0.3f };
        var responsePayload = new { data = new[] { new { index = 0, embedding = expectedEmbedding } } };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((r, c) => capturedRequest = r)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(responsePayload))
            });

        var service = CreateService();

        // Act
        await service.GenerateEmbeddingAsync("Test text");

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal("Bearer", capturedRequest.Headers.Authorization.Scheme);
        Assert.Equal("test-api-key", capturedRequest.Headers.Authorization.Parameter);
        
        var requestBody = await capturedRequest.Content.ReadAsStringAsync();
        Assert.Contains("\"model\":\"nvidia/nemotron-3-embed-1b:free\"", requestBody);
        Assert.Contains("Test text", requestBody);
    }
}
