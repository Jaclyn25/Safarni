using ChatBotApi.Application.Features.Chat.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace Safarni.Application.UnitTests.Chat;

public class GooglePlacesServiceTests
{
    [Fact]
    public async Task TextSearchAsync_ShouldReturnEmptyWithoutSendingRequestWhenApiKeyIsMissing()
    {
        var handler = new TrackingHandler();
        var configuration = new Mock<IConfiguration>();
        var service = new GooglePlacesService(
            new HttpClient(handler),
            configuration.Object,
            Mock.Of<ILogger<GooglePlacesService>>());

        var result = await service.TextSearchAsync("hotels in Rome");

        result.Should().BeEmpty();
        handler.RequestCount.Should().Be(0);
    }

    [Fact]
    public async Task TextSearchAsync_ShouldPropagateCallerCancellation()
    {
        var handler = new TrackingHandler();
        var configuration = new Mock<IConfiguration>();
        configuration
            .Setup(x => x["GooglePlaces:ApiKey"])
            .Returns("test-key");
        var service = new GooglePlacesService(
            new HttpClient(handler),
            configuration.Object,
            Mock.Of<ILogger<GooglePlacesService>>());
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var action = () => service.TextSearchAsync(
            "hotels in Rome",
            cancellation.Token,
            "en");

        await action.Should().ThrowAsync<OperationCanceledException>();
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("legacy-reference", "photo_reference=legacy-reference")]
    [InlineData("places/abc/photos/def", "places/abc/photos/def/media")]
    public void GetPhotoUrl_ShouldHandleMissingAndBothReferenceFormats(
        string reference,
        string expectedFragment)
    {
        var configuration = new Mock<IConfiguration>();
        configuration
            .Setup(x => x["GooglePlaces:ApiKey"])
            .Returns("test-key");
        var service = new GooglePlacesService(
            new HttpClient(new TrackingHandler()),
            configuration.Object,
            Mock.Of<ILogger<GooglePlacesService>>());

        var result = service.GetPhotoUrl(reference);

        if (string.IsNullOrEmpty(expectedFragment))
        {
            result.Should().BeEmpty();
        }
        else
        {
            result.Should().Contain(expectedFragment);
        }
    }

    private sealed class TrackingHandler : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("""{"places":[]}""")
            });
        }
    }
}
