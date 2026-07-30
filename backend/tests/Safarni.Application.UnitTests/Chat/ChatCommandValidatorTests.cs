using ChatBotApi.Application.Features.Chat.Commands.CreateConversation;
using ChatBotApi.Application.Features.Chat.Commands.SendMessage;
using ChatBotApi.Application.Features.Chat.Commands.RegenerateResponse;
using FluentAssertions;

namespace Safarni.Application.UnitTests.Chat;

public class ChatCommandValidatorTests
{
    private readonly SendMessageCommandValidator _sendValidator = new();
    private readonly CreateConversationCommandValidator _createValidator = new();
    private readonly RegenerateResponseCommandValidator _regenerateValidator = new();

    [Fact]
    public async Task SendMessage_ShouldRejectMissingIdentifiersAndBlankMessage()
    {
        var result = await _sendValidator.ValidateAsync(
            new SendMessageCommand(Guid.Empty, string.Empty, "   "));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "ConversationId");
        result.Errors.Should().Contain(error => error.PropertyName == "UserId");
        result.Errors.Should().Contain(error => error.PropertyName == "Message");
    }

    [Fact]
    public async Task SendMessage_ShouldAcceptExactlyFourThousandCharacters()
    {
        var result = await _sendValidator.ValidateAsync(
            new SendMessageCommand(Guid.NewGuid(), "user-1", new string('س', 4000)));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task SendMessage_ShouldRejectMoreThanFourThousandCharacters()
    {
        var result = await _sendValidator.ValidateAsync(
            new SendMessageCommand(Guid.NewGuid(), "user-1", new string('س', 4001)));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => error.PropertyName == "Message");
    }

    [Fact]
    public async Task CreateConversation_ShouldRejectMissingUserIdAndOversizedTitle()
    {
        var result = await _createValidator.ValidateAsync(
            new CreateConversationCommand(string.Empty, new string('x', 201)));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "UserId");
        result.Errors.Should().Contain(error => error.PropertyName == "Title");
    }

    [Fact]
    public async Task RegenerateResponse_ShouldRejectMissingConversationAndUserIdentifiers()
    {
        var result = await _regenerateValidator.ValidateAsync(
            new RegenerateResponseCommand(Guid.Empty, string.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "ConversationId");
        result.Errors.Should().Contain(error => error.PropertyName == "UserId");
    }
}
