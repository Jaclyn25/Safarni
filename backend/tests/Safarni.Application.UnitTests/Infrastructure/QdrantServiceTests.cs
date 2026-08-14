using System.Net;
using System.Text;
using ChatBotApi.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace Safarni.Application.UnitTests.Infrastructure;

public sealed class QdrantServiceTests
{
    [Fact]
    public async Task Search_ShouldApplyThresholdAndPreserveScoreAndSource()
    {
        string? requestBody = null;
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, _) =>
                requestBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {"result":[{"id":"11111111-1111-1111-1111-111111111111","score":0.83,"payload":{"entityId":"7","entityType":"VisaInfo","content":"Verified visa guidance","source":"SQL Server","sourceUrl":"https://example.gov/visa","city":"","country":"Turkey","createdDate":"2026-08-13T00:00:00Z"}}]}
                    """,
                    Encoding.UTF8,
                    "application/json")
            });

        var service = CreateService(handler.Object, new Dictionary<string, string?>
        {
            ["RAG:QdrantVectorSize"] = "3",
            ["RAG:ScoreThreshold"] = "0.60"
        });

        var results = await service.SearchSimilarChunksAsync([0.1f, 0.2f, 0.3f], 5);

        results.Should().ContainSingle();
        results[0].Score.Should().BeApproximately(0.83f, 0.001f);
        results[0].SourceUrl.Should().Be("https://example.gov/visa");
        requestBody.Should().Contain("\"score_threshold\":0.6");
        requestBody.Should().Contain("\"limit\":5");
    }

    [Fact]
    public async Task Search_WithWrongVectorDimension_ShouldFailBeforeHttpCall()
    {
        var handler = new Mock<HttpMessageHandler>();
        var service = CreateService(handler.Object, new Dictionary<string, string?>
        {
            ["RAG:QdrantVectorSize"] = "3"
        });

        var action = () => service.SearchSimilarChunksAsync([0.1f, 0.2f], 3);

        await action.Should().ThrowAsync<ArgumentException>();
        handler.Protected().Verify(
            "SendAsync",
            Times.Never(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task DeleteChunksByEntityId_ShouldUsePayloadFilter()
    {
        string? requestBody = null;
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(request =>
                    request.Method == HttpMethod.Post &&
                    request.RequestUri!.AbsolutePath.EndsWith("/points/delete")),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, _) =>
                requestBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var service = CreateService(handler.Object, new Dictionary<string, string?>());
        await service.DeleteChunksByEntityIdAsync("42");

        requestBody.Should().Contain("\"key\":\"entityId\"");
        requestBody.Should().Contain("\"value\":\"42\"");
    }

    private static QdrantService CreateService(
        HttpMessageHandler handler,
        IDictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
        return new QdrantService(
            new HttpClient(handler),
            configuration,
            new Mock<ILogger<QdrantService>>().Object);
    }
}
