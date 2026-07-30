using ChatBotApi.Application.Common.Interfaces;
using ChatBotApi.Application.Features.Chat.Commands.CreateConversation;
using ChatBotApi.Domain.Entities;
using FluentAssertions;
using Moq;

namespace Safarni.Application.UnitTests.Chat;

public class CreateConversationCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldCreateEmptyConversationWithoutHardcodedAssistantMessage()
    {
        var repository = new Mock<IChatRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        Conversation? savedConversation = null;
        repository
            .Setup(x => x.AddAsync(
                It.IsAny<Conversation>(),
                It.IsAny<CancellationToken>()))
            .Callback<Conversation, CancellationToken>((conversation, _) =>
                savedConversation = conversation)
            .Returns(Task.CompletedTask);
        unitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        var handler = new CreateConversationCommandHandler(
            repository.Object,
            unitOfWork.Object);

        var result = await handler.Handle(
            new CreateConversationCommand("user-1", null),
            CancellationToken.None);

        result.Title.Should().Be("محادثة جديدة");
        result.Messages.Should().BeEmpty();
        savedConversation.Should().NotBeNull();
        savedConversation!.Messages.Should().BeEmpty();
        unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
