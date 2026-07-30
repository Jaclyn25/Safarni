using ChatBotApi.Application.Features.Chat.Services;
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
    }

    [Theory]
    [InlineData("عامل إيه؟")]
    [InlineData("انت غبي")]
    [InlineData("شكراً")]
    [InlineData("اكتب لي كود بايثون")]
    public void BuildSystemPrompt_ShouldLetModelComposeSocialAndScopeResponses(string message)
    {
        var prompt = _builder.BuildSystemPrompt([], message);

        prompt.Should().Contain("Normal conversation is allowed");
        prompt.Should().Contain("For an out-of-scope request");
        prompt.Should().Contain("Match the user's level of formality");
        prompt.Should().Contain("Vary openings");
    }
}
