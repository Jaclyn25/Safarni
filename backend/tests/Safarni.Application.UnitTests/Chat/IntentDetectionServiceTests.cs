using ChatBotApi.Application.Common.Interfaces;
using ChatBotApi.Application.Features.Chat.Services;
using FluentAssertions;

namespace Safarni.Application.UnitTests.Chat;

public class IntentDetectionServiceTests
{
    private readonly IntentDetectionService _service = new();

    [Theory]
    [InlineData("اكتب لي كود بايثون")]
    [InlineData("what is 24 * 7?")]
    [InlineData("ما نتيجة المباراة؟")]
    [InlineData("Give me a medical diagnosis")]
    public void DetectIntent_ShouldRejectClearlyUnrelatedQuestions(string message)
    {
        _service.DetectIntent(message).Should().Be(ChatIntent.OutOfScope);
    }

    [Theory]
    [InlineData("خطط لي رحلة إلى باريس")]
    [InlineData("What should I know before visiting Poland?")]
    [InlineData("احكي لي أكتر")]
    [InlineData("What about there?")]
    [InlineData("هل مهرجان كرة القدم في باريس يستحق الزيارة؟")]
    public void DetectIntent_ShouldKeepTravelAndFollowUpQuestionsInScope(string message)
    {
        _service.DetectIntent(message).Should().NotBe(ChatIntent.OutOfScope);
    }

    [Theory]
    [InlineData("عامل اي")]
    [InlineData("عامل إيه؟")]
    [InlineData("أخبارك")]
    [InlineData("انت غبي")]
    [InlineData("شكراً")]
    [InlineData("Who are you?")]
    public void DetectIntent_ShouldTreatGreetingsAndFeedbackAsSocialConversation(string message)
    {
        _service.DetectIntent(message).Should().Be(ChatIntent.Social);
    }

    [Theory]
    [InlineData("رشح مطاعم في القاهرة")]
    [InlineData("hotels near the airport in Rome")]
    public void DetectIntent_ShouldRoutePlaceSearchesToPlaces(string message)
    {
        _service.DetectIntent(message)
            .Should().BeOneOf(ChatIntent.GooglePlacesOnly, ChatIntent.Hybrid);
    }

    [Fact]
    public void DetectIntent_ShouldNotTreatAWordContainingHiAsAGreeting()
    {
        _service.DetectIntent("this is a software debugging question")
            .Should().Be(ChatIntent.OutOfScope);
    }
}
