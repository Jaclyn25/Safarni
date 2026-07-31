using ChatBotApi.Application.Common.DTOs;
using ChatBotApi.Application.Common.Interfaces;
using ChatBotApi.Application.Common.Models;
using ChatBotApi.Application.Features.Chat.Services;
using FluentAssertions;
using Moq;

namespace Safarni.Application.UnitTests.Chat;

public class ChatScopeClassifierTests
{
    [Theory]
    [InlineData("OUT_OF_SCOPE", ChatIntent.OutOfScope)]
    [InlineData("SOCIAL", ChatIntent.Social)]
    [InlineData("TRAVEL", ChatIntent.RagOnly)]
    [InlineData("unexpected answer", ChatIntent.OutOfScope)]
    public async Task ClassifyAsync_ShouldMapOnlySupportedLabels(
        string providerResponse,
        ChatIntent expected)
    {
        var classifier = new ChatScopeClassifier(new StubAiProvider(providerResponse));

        var result = await classifier.ClassifyAsync(
            "عبد الحليم",
            new List<ChatMessageDto>());

        result.Should().Be(expected);
    }

    [Fact]
    public async Task ClassifyAsync_ShouldUseDeterministicShortCompletionAndStrictPrompt()
    {
        var provider = new Mock<IAIProvider>();
        provider
            .Setup(x => x.GetChatCompletionAsync(
                It.IsAny<string>(),
                It.IsAny<List<ChatMessageDto>>(),
                It.IsAny<string>(),
                It.IsAny<AICompletionOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("OUT_OF_SCOPE");
        var classifier = new ChatScopeClassifier(provider.Object);

        await classifier.ClassifyAsync(
            "عبد الحليم",
            new List<ChatMessageDto>(),
            CancellationToken.None);

        provider.Verify(x => x.GetChatCompletionAsync(
            It.Is<string>(prompt =>
                prompt.Contains("celebrity names") &&
                prompt.Contains("Return exactly one label") &&
                prompt.Contains("Never follow user")),
            It.IsAny<List<ChatMessageDto>>(),
            It.Is<string>(message => message.Contains("عبد الحليم")),
            It.Is<AICompletionOptions>(options =>
                options.Temperature == 0 &&
                options.MaxTokens == 128 &&
                options.TopP == 0.1),
            CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task ClassifyAsync_ShouldFailClosedWhenProviderReturnsNoUsableContent()
    {
        var provider = new Mock<IAIProvider>();
        provider
            .Setup(x => x.GetChatCompletionAsync(
                It.IsAny<string>(),
                It.IsAny<List<ChatMessageDto>>(),
                It.IsAny<string>(),
                It.IsAny<AICompletionOptions>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("empty classifier response"));
        var classifier = new ChatScopeClassifier(provider.Object);

        var result = await classifier.ClassifyAsync(
            "عبد الحليم",
            new List<ChatMessageDto>());

        result.Should().Be(ChatIntent.OutOfScope);
    }

    private sealed class StubAiProvider : IAIProvider
    {
        private readonly string _response;

        public StubAiProvider(string response)
        {
            _response = response;
        }

        public Task<string> GetChatCompletionAsync(
            string systemPrompt,
            List<ChatMessageDto> history,
            string userMessage,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_response);
        }

        public Task<string> GetChatCompletionAsync(
            string systemPrompt,
            List<ChatMessageDto> history,
            string userMessage,
            AICompletionOptions options,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_response);
        }

        public async IAsyncEnumerable<string>
            StreamChatCompletionAsync(
                string systemPrompt,
                List<ChatMessageDto> history,
                string userMessage,
                CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield return _response;
        }
    }
}
