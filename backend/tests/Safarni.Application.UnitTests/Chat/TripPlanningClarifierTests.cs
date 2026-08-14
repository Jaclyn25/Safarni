using ChatBotApi.Application.Common.DTOs;
using ChatBotApi.Application.Features.Chat.Services;
using FluentAssertions;

namespace Safarni.Application.UnitTests.Chat;

public class TripPlanningClarifierTests
{
    [Fact]
    public void BuildClarification_ShouldAskOneArabicTimingQuestionWithoutRequiringEveryPreference()
    {
        var response = TripPlanningClarifier.BuildClarification(
            true,
            "أنا عاوز أروح تركيا",
            []);

        response.Should().NotBeNull();
        response.Should().Contain("هتسافر إمتى");
        response.Should().NotContain("ميزانيتك");
        response.Should().NotContain("مسافر لوحدك");
        response.Should().Contain("---SUGGESTIONS---");
    }

    [Fact]
    public void BuildClarification_ShouldAskOnlyForDestinationWhenItIsUnknown()
    {
        var response = TripPlanningClarifier.BuildClarification(
            true,
            "Plan a trip for me",
            []);

        response.Should().NotBeNull();
        response.Should().Contain("where would you like to go");
        response.Should().NotContain("budget");
        response.Should().Contain("Surprise me with a destination");
    }

    [Fact]
    public void BuildClarification_ShouldProceedWithDefaultsAfterDestinationAndTimingAreKnown()
    {
        TripPlanningClarifier.BuildClarification(
                true,
                "Plan five days in Paris in September",
                [])
            .Should().BeNull();
    }

    [Fact]
    public void BuildClarification_ShouldIgnoreQuickTravelFacts()
    {
        TripPlanningClarifier.BuildClarification(
                false,
                "ما عاصمة تركيا؟",
                [])
            .Should().BeNull();
    }

    [Fact]
    public void BuildClarification_ShouldRememberDestinationFromEarlierTurns()
    {
        var history = new List<ChatMessageDto>
        {
            new() { Role = "user", Content = "Plan a trip to Paris" },
            new() { Role = "assistant", Content = "When would you like to go?" }
        };

        TripPlanningClarifier.BuildClarification(
                true,
                "Use a flexible 5-day plan",
                history)
            .Should().BeNull();
    }
}
