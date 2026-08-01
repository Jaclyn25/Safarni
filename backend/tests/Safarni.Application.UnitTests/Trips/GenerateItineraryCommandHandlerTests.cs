using FluentAssertions;
using Moq;
using Safarni.Application.Abstractions.Authentication;
using Safarni.Application.Common.Results;
using Safarni.Application.Features.Trips.GenerateItinerary;
using Xunit;

namespace Safarni.Application.UnitTests.Trips;

public class GenerateItineraryCommandHandlerTests
{
    [Fact]
    public async Task Handle_Should_DelegateToProcessor_ForAuthenticatedUser()
    {
        var userId = Guid.NewGuid();
        var tripId = Guid.NewGuid();
        var currentUser = new Mock<ICurrentUserService>();
        var processor = new Mock<IItineraryGenerationProcessor>();

        currentUser
            .Setup(service => service.UserId)
            .Returns(userId);

        var response = CreateResponse(tripId);
        processor
            .Setup(service => service.GenerateAsync(
                tripId,
                userId,
                It.IsAny<GenerateItineraryPreferences>(),
                2500m,
                "USD",
                2,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ItineraryResponse>.Success(response));

        var handler = new GenerateItineraryCommandHandler(
            currentUser.Object,
            processor.Object);

        var result = await handler.Handle(
            new GenerateItineraryCommand(
                tripId,
                CreatePreferences(),
                2500m,
                "USD",
                2),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(response);

        processor.Verify(service => service.GenerateAsync(
            tripId,
            userId,
            It.IsAny<GenerateItineraryPreferences>(),
            2500m,
            "USD",
            2,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_ReturnAuthenticationFailure_WhenUserMissing()
    {
        var currentUser = new Mock<ICurrentUserService>();
        var processor = new Mock<IItineraryGenerationProcessor>();

        currentUser
            .Setup(service => service.UserId)
            .Returns((Guid?)null);

        var handler = new GenerateItineraryCommandHandler(
            currentUser.Object,
            processor.Object);

        var result = await handler.Handle(
            new GenerateItineraryCommand(
                Guid.NewGuid(),
                CreatePreferences(),
                null,
                "USD",
                1),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(
            "Auth.AuthenticationRequired");

        processor.VerifyNoOtherCalls();
    }

    [Fact]
    public void Validator_Should_AllowNoBudget_AndRejectInvalidTravelerCount()
    {
        var validator = new GenerateItineraryCommandValidator();

        var valid = validator.Validate(
            new GenerateItineraryCommand(
                Guid.NewGuid(),
                CreatePreferences(),
                null,
                "USD",
                1));

        valid.IsValid.Should().BeTrue();

        var invalid = validator.Validate(
            new GenerateItineraryCommand(
                Guid.NewGuid(),
                CreatePreferences(),
                null,
                "USD",
                0));

        invalid.IsValid.Should().BeFalse();
        invalid.Errors.Should().Contain(error =>
            error.PropertyName == "TravelerCount");
    }

    private static GenerateItineraryPreferences CreatePreferences()
    {
        return new GenerateItineraryPreferences
        {
            Interests = ["culture", "food"],
            Pace = "balanced",
            Transportation = ["walking", "public_transport"],
            AccommodationStyle = "mid_range",
            PreferredLanguage = "en",
            AdditionalNotes = null
        };
    }

    private static ItineraryResponse CreateResponse(Guid tripId)
    {
        var now = DateTime.UtcNow;

        return new ItineraryResponse(
            Guid.NewGuid(),
            tripId,
            CreatePreferences(),
            null,
            "USD",
            2,
            "ready",
            "ai",
            true,
            now,
            now,
            0m,
            0,
            true,
            []);
    }
}
