using FluentAssertions;
using Moq;
using Safarni.Application.Abstractions.Authentication;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Abstractions.Storage;
using Safarni.Application.Features.Community.Commands;
using Safarni.Domain.Entities;
using Safarni.Domain.Enums;
using Xunit;

namespace Safarni.Application.UnitTests.Community;

public sealed class CommunityPostCommandHandlerTests
{
    [Fact]
    public async Task CreatePost_ShouldRejectUnauthenticatedUser()
    {
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(service => service.UserId).Returns((Guid?)null);
        var repository = new Mock<ICommunityRepository>();
        var handler = new CreateCommunityPostCommandHandler(repository.Object, currentUser.Object);

        var result = await handler.Handle(
            new CreateCommunityPostCommand(CommunityPostType.TravelStory, "A story", "A useful travel story.", null, null, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Community.Unauthorized");
        repository.Verify(repo => repo.AddPostAsync(It.IsAny<CommunityPost>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ToggleLike_ShouldAddInteractionAndIncrementCount()
    {
        var userId = Guid.NewGuid();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(service => service.UserId).Returns(userId);
        var post = new CommunityPost
        {
            Id = Guid.NewGuid(),
            Status = CommunityPostStatus.Published,
            LikesCount = 4,
            Author = new User { Id = Guid.NewGuid(), FullName = "Author", IsActive = true }
        };
        var repository = new Mock<ICommunityRepository>();
        repository.Setup(repo => repo.GetPostWithInteractionsAsync(post.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);
        var handler = new ToggleCommunityPostInteractionCommandHandler(repository.Object, currentUser.Object);

        var result = await handler.Handle(
            new ToggleCommunityPostInteractionCommand(post.Id, CommunityPostInteractionType.Like),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsActive.Should().BeTrue();
        result.Value.NewCount.Should().Be(5);
        post.LikesCount.Should().Be(5);
        repository.Verify(repo => repo.AddInteractionAsync(It.Is<CommunityPostInteraction>(interaction =>
            interaction.PostId == post.Id && interaction.UserId == userId && interaction.Type == CommunityPostInteractionType.Like), It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddComment_ShouldRejectReplyFromAnotherPost()
    {
        var userId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(service => service.UserId).Returns(userId);
        var repository = new Mock<ICommunityRepository>();
        repository.Setup(repo => repo.GetPostWithInteractionsAsync(postId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommunityPost { Id = postId, Status = CommunityPostStatus.Published });
        repository.Setup(repo => repo.GetCommentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommunityPostComment { Id = Guid.NewGuid(), PostId = Guid.NewGuid() });
        var handler = new CreateCommunityCommentCommandHandler(repository.Object, currentUser.Object);

        var result = await handler.Handle(
            new CreateCommunityCommentCommand(postId, "Helpful reply", Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Community.InvalidParentComment");
        repository.Verify(repo => repo.AddCommentAsync(It.IsAny<CommunityPostComment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UploadMedia_ShouldStoreSupportedImage()
    {
        var storage = new Mock<ICommunityMediaStorage>();
        storage.Setup(service => service.SaveAsync(It.IsAny<Stream>(), ".jpg", It.IsAny<CancellationToken>()))
            .ReturnsAsync("/uploads/community-media/photo.jpg");
        var handler = new UploadCommunityMediaCommandHandler(storage.Object);
        var bytes = new byte[] { 1, 2, 3 };

        var result = await handler.Handle(
            new UploadCommunityMediaCommand("kyoto.jpg", "image/jpeg", bytes.Length, bytes),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.MediaType.Should().Be("image");
        result.Value.Url.Should().Be("/uploads/community-media/photo.jpg");
        storage.Verify(service => service.SaveAsync(It.IsAny<Stream>(), ".jpg", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadMedia_ShouldRejectUnsupportedExtension()
    {
        var storage = new Mock<ICommunityMediaStorage>();
        var handler = new UploadCommunityMediaCommandHandler(storage.Object);

        var result = await handler.Handle(
            new UploadCommunityMediaCommand("script.exe", "application/octet-stream", 3, new byte[] { 1, 2, 3 }),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Community.InvalidMedia");
        storage.Verify(service => service.SaveAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
