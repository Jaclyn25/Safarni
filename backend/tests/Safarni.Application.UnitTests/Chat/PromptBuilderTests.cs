using ChatBotApi.Application.Features.Chat.Services;
using ChatBotApi.Application.Common.Interfaces;
using FluentAssertions;

namespace Safarni.Application.UnitTests.Chat;

public class PromptBuilderTests
{
    private readonly PromptBuilder _builder = new();

    [Fact]
    public void BuildSystemPrompt_ShouldContainStrictScopeRulesAndDatabaseContext()
    {
        var prompt = _builder.BuildSystemPrompt([], "رتب لي رحلة إلى اليابان");

        prompt.Should().Contain("You are Safarni AI");
        prompt.Should().Contain("Only handle travel, tourism, destinations, trip planning");
        prompt.Should().Contain("Answer strictly in Arabic with a warm, helpful tone");
        prompt.Should().Contain("Use Safarni Knowledge as the primary source");
        prompt.Should().Contain("Safarni Knowledge:");
        prompt.Should().Contain("No relevant Safarni knowledge was retrieved");
        prompt.Should().Contain("visas, border rules, safety alerts");
    }

    [Theory]
    [InlineData("عامل إيه؟")]
    [InlineData("انت غبي")]
    [InlineData("شكراً")]
    public void BuildDirectSystemPrompt_ShouldTreatSocialMessagesAsSmallTalk(string message)
    {
        var prompt = _builder.BuildDirectSystemPrompt(
            ChatIntent.Social,
            message);

        prompt.Should().Contain("You are Safarni AI");
        prompt.Should().Contain("Answer strictly in Arabic with a warm, concise, helpful tone");
        prompt.Should().Contain("The user is making small talk, greeting you, or reacting to the conversation");
        prompt.Should().Contain("friendly human travel companion");
        prompt.Should().Contain("light playful line or emoji");
        prompt.Should().NotContain("politely decline");
    }

    [Theory]
    [InlineData(ChatIntent.OutOfScope)]
    [InlineData(ChatIntent.OutOfDomain)]
    public void BuildDirectSystemPrompt_ShouldDeclineNonTravelTopics(ChatIntent intent)
    {
        var prompt = _builder.BuildDirectSystemPrompt(intent, "اكتب لي كود بايثون");

        prompt.Should().Contain("You are Safarni AI");
        prompt.Should().Contain("Answer strictly in Arabic with a warm, concise, helpful tone");
        prompt.Should().Contain("politely decline and explain that you can only help with Safarni travel-related questions");
    }

    [Theory]
    [InlineData(ChatIntent.Ambiguous)]
    [InlineData(ChatIntent.RagOnly)]
    [InlineData(ChatIntent.GooglePlacesOnly)]
    [InlineData(ChatIntent.Hybrid)]
    public void BuildDirectSystemPrompt_ShouldFallBackToDeclineForRoutingOnlyIntents(ChatIntent intent)
    {
        var prompt = _builder.BuildDirectSystemPrompt(intent, "test");

        prompt.Should().Contain("politely decline and explain that you can only help with Safarni travel-related questions");
    }

    [Theory]
    [InlineData("مرحبا", "Answer strictly in Arabic with a warm, helpful tone")]
    [InlineData("Hello", "Answer strictly in English with a warm, helpful tone")]
    [InlineData("Paris فين؟", "Answer strictly in Arabic with a warm, helpful tone")]
    public void BuildSystemPrompt_ShouldAlwaysAnswerInArabic(string message, string expectedRule)
    {
        _builder.BuildSystemPrompt([], message).Should().Contain(expectedRule);
    }

    [Theory]
    [InlineData("مرحبا", "ar")]
    [InlineData("Hello, where is the Eiffel Tower?", "en")]
    [InlineData("مزيج mixed message", "ar")]
    public void GetGooglePlacesLanguageCode_ShouldDetectArabicScript(string message, string expectedCode)
    {
        _builder.GetGooglePlacesLanguageCode(message).Should().Be(expectedCode);
    }

    [Fact]
    public void BuildUserPrompt_ShouldReinforceArabicAndRequireACompleteAnswer()
    {
        var prompt = _builder.BuildUserPrompt("ما عاصمة تركيا؟");

        prompt.Should().Contain("entire response in Arabic");
        prompt.Should().Contain("Natural Arabic response at the appropriate length:");
    }

    [Fact]
    public void BuildSystemPrompt_ShouldForbidVisaClaimsWithoutVerifiedVisaKnowledge()
    {
        var prompt = _builder.BuildSystemPrompt(
            [],
            "ما متطلبات التأشيرة للسعودية من مصر؟");

        prompt.Should().Contain("VISA MODE — NO VERIFIED VISA SOURCE");
        prompt.Should().Contain("cannot currently verify");
        prompt.Should().Contain("Do not claim that an e-visa is available");
    }

    [Fact]
    public void BuildSystemPrompt_ShouldRequireAdaptiveLengthAndAvoidDefaultTables()
    {
        var prompt = _builder.BuildSystemPrompt([], "ما عاصمة تركيا؟");

        prompt.Should().Contain("A quick fact gets 1–3 sentences");
        prompt.Should().Contain("Never use a Markdown table in a chat response");
        prompt.Should().Contain("ask at most one high-value question at a time");
        prompt.Should().Contain("warm, perceptive travel companion");
    }
}
