using ChatBotApi.Application.Features.Chat.Services;
using ChatBotApi.Application.Common.Interfaces;
using FluentAssertions;

namespace Safarni.Application.UnitTests.Chat;

public class PromptBuilderTests
{
    private readonly PromptBuilder _builder = new();

    [Fact]
    public void BuildSystemPrompt_ShouldContainStrictScopeAndNaturalToneRules()
    {
        var prompt = _builder.BuildSystemPrompt([], "رتب لي رحلة إلى اليابان");

        prompt.Should().Contain("SCOPE GATE");
        prompt.Should().Contain("do not answer any factual part");
        prompt.Should().Contain("Vary openings");
        prompt.Should().Contain("answer strictly in Arabic");
        prompt.Should().Contain("do not omit useful details");
        prompt.Should().Contain("standalone name of a person");
        prompt.Should().Contain("Never provide a biography");
    }

    [Theory]
    [InlineData("عامل إيه؟")]
    [InlineData("انت غبي")]
    [InlineData("شكراً")]
    public void BuildDirectSystemPrompt_ShouldKeepSocialMessagesOutOfScopeExempt(string message)
    {
        var prompt = _builder.BuildDirectSystemPrompt(
            ChatIntent.Social,
            message);

        prompt.Should().Contain("allowed SOCIAL CONVERSATION");
        prompt.Should().Contain("do not apply the out-of-scope response rule");
        prompt.Should().Contain("Do not mention scope");
        prompt.Should().Contain("Compose the response yourself");
    }

    [Fact]
    public void BuildDirectSystemPrompt_ShouldMakeModelRedirectOutOfScopeWithoutFixedAnswer()
    {
        var prompt = _builder.BuildDirectSystemPrompt(
            ChatIntent.OutOfScope,
            "اكتب لي كود بايثون");

        prompt.Should().Contain("OUT OF SCOPE");
        prompt.Should().Contain("Do not answer its factual");
        prompt.Should().Contain("Generate a brief, friendly redirection");
        prompt.Should().Contain("No fixed answer text is provided");
    }

    [Theory]
    [InlineData(ChatIntent.Ambiguous)]
    [InlineData(ChatIntent.RagOnly)]
    [InlineData(ChatIntent.GooglePlacesOnly)]
    [InlineData(ChatIntent.Hybrid)]
    public void BuildDirectSystemPrompt_ShouldRejectUnsupportedRoutingIntents(ChatIntent intent)
    {
        var action = () => _builder.BuildDirectSystemPrompt(intent, "test");

        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData("مرحبا", "answer strictly in Arabic")]
    [InlineData("Hello", "answer strictly in English")]
    [InlineData("Paris فين؟", "answer strictly in Arabic")]
    public void BuildSystemPrompt_ShouldChooseTheUsersDominantLanguage(
        string message,
        string expectedRule)
    {
        _builder.BuildSystemPrompt([], message).Should().Contain(expectedRule);
    }
}
