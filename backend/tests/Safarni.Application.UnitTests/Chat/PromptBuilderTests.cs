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
        prompt.Should().Contain("CRITICAL RULE 1");
        prompt.Should().Contain("ONLY answer questions that are directly related to tourism");
        prompt.Should().Contain("Answer strictly in Arabic with a warm, helpful tone");
        prompt.Should().Contain("CRITICAL RULE 2");
        prompt.Should().Contain("Use the Database Context below as your ONLY source of factual truth");
        prompt.Should().Contain("Database Context:");
        prompt.Should().Contain("No Safarni-specific context found");
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
        prompt.Should().Contain("The user is making small talk or greeting you");
        prompt.Should().Contain("travel, destinations, itineraries, hotels, attractions, or trip planning");
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
    [InlineData("Hello", "Answer strictly in Arabic with a warm, helpful tone")]
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
}
