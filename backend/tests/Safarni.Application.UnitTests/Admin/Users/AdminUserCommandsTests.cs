using FluentAssertions;
using Moq;
using Safarni.Application.Abstractions.Auditing;
using Safarni.Application.Abstractions.Authentication;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Features.Admin.Common;
using Safarni.Application.Features.Admin.Users;
using Safarni.Domain.Entities;
using Safarni.Domain.Enums;

namespace Safarni.Application.UnitTests.Admin.Users;

public class AdminUserCommandsTests
{
    private static readonly Guid ActorId = Guid.NewGuid();
    private static readonly Guid TargetId = Guid.NewGuid();

    private static User CreateUser(
        Guid id,
        UserRole role = UserRole.User,
        bool isActive = true)
    {
        return new User
        {
            Id = id,
            FullName = "Test User",
            Email = "test@example.com",
            Role = role,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static Mock<ICurrentUserService> CreateCurrentUser(
        Guid userId)
    {
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(x => x.UserId).Returns(userId);
        currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);
        return currentUser;
    }

    private static Mock<IAdminRepository> CreateAdminRepository()
    {
        var repository = new Mock<IAdminRepository>();
        repository
            .Setup(x => x.GetUserCountsAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AdminUserCounts(1, 0, 2, 5));
        return repository;
    }

    private static Mock<IAuditLogWriter> CreateAuditWriter()
    {
        var writer = new Mock<IAuditLogWriter>();
        writer
            .Setup(x => x.WriteAsync(
                It.IsAny<Guid?>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<object?>(),
                It.IsAny<AuditSeverity>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return writer;
    }

    [Fact]
    public async Task UpdateRoles_WhenAdminDemotesSelf_ShouldFail()
    {
        var userRepository = new Mock<IUserRepository>();
        var handler = new UpdateUserRolesCommandHandler(
            userRepository.Object,
            CreateAdminRepository().Object,
            CreateAuditWriter().Object,
            CreateCurrentUser(ActorId).Object);

        var result = await handler.Handle(
            new UpdateUserRolesCommand(ActorId, UserRole.Support),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(AdminErrors.SelfRoleChangeNotAllowed.Code);
        userRepository.Verify(
            x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateRoles_WhenRoleInvalid_ShouldFail()
    {
        var handler = new UpdateUserRolesCommandHandler(
            new Mock<IUserRepository>().Object,
            CreateAdminRepository().Object,
            CreateAuditWriter().Object,
            CreateCurrentUser(ActorId).Object);

        var result = await handler.Handle(
            new UpdateUserRolesCommand(TargetId, (UserRole)99),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(AdminErrors.InvalidRole.Code);
    }

    [Fact]
    public async Task UpdateRoles_WhenUserNotFound_ShouldFail()
    {
        var userRepository = new Mock<IUserRepository>();
        userRepository
            .Setup(x => x.GetByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var handler = new UpdateUserRolesCommandHandler(
            userRepository.Object,
            CreateAdminRepository().Object,
            CreateAuditWriter().Object,
            CreateCurrentUser(ActorId).Object);

        var result = await handler.Handle(
            new UpdateUserRolesCommand(TargetId, UserRole.Analyst),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(AdminErrors.UserNotFound.Code);
    }

    [Fact]
    public async Task UpdateRoles_WhenValid_ShouldChangeRoleAndAudit()
    {
        var user = CreateUser(TargetId);
        var userRepository = new Mock<IUserRepository>();
        userRepository
            .Setup(x => x.GetByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var auditWriter = CreateAuditWriter();

        var handler = new UpdateUserRolesCommandHandler(
            userRepository.Object,
            CreateAdminRepository().Object,
            auditWriter.Object,
            CreateCurrentUser(ActorId).Object);

        var result = await handler.Handle(
            new UpdateUserRolesCommand(TargetId, UserRole.ContentManager),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.Role.Should().Be(UserRole.ContentManager);
        userRepository.Verify(
            x => x.UpdateAsync(user, It.IsAny<CancellationToken>()),
            Times.Once);
        auditWriter.Verify(
            x => x.WriteAsync(
                ActorId,
                AdminAuditActions.UserRoleChanged,
                "User",
                TargetId.ToString(),
                It.IsAny<object?>(),
                It.IsAny<AuditSeverity>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        result.Value!.Role.Should().Be(UserRole.ContentManager);
        result.Value.TripsCount.Should().Be(1);
    }

    [Fact]
    public async Task UpdateRoles_WhenRoleUnchanged_ShouldNotSaveUser()
    {
        var user = CreateUser(TargetId, role: UserRole.Admin);
        var userRepository = new Mock<IUserRepository>();
        userRepository
            .Setup(x => x.GetByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var handler = new UpdateUserRolesCommandHandler(
            userRepository.Object,
            CreateAdminRepository().Object,
            CreateAuditWriter().Object,
            CreateCurrentUser(ActorId).Object);

        var result = await handler.Handle(
            new UpdateUserRolesCommand(TargetId, UserRole.Admin),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        userRepository.Verify(
            x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateRoles_WhenAssigningTravelAgencyDirectly_ShouldFail()
    {
        var user = CreateUser(TargetId);
        var userRepository = new Mock<IUserRepository>();
        userRepository
            .Setup(x => x.GetByIdAsync(
                TargetId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        var handler = new UpdateUserRolesCommandHandler(
            userRepository.Object,
            CreateAdminRepository().Object,
            CreateAuditWriter().Object,
            CreateCurrentUser(ActorId).Object);

        var result = await handler.Handle(
            new UpdateUserRolesCommand(
                TargetId,
                UserRole.TravelAgency),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(
            AdminErrors.AgencyRoleManagedByAgencyWorkflow);
        userRepository.Verify(
            x => x.UpdateAsync(
                It.IsAny<User>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateStatus_WhenAdminDeactivatesSelf_ShouldFail()
    {
        var handler = new UpdateUserStatusCommandHandler(
            new Mock<IUserRepository>().Object,
            CreateAdminRepository().Object,
            CreateAuditWriter().Object,
            CreateCurrentUser(ActorId).Object);

        var result = await handler.Handle(
            new UpdateUserStatusCommand(ActorId, IsActive: false),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(AdminErrors.SelfDeactivationNotAllowed.Code);
    }

    [Fact]
    public async Task UpdateStatus_WhenUserNotFound_ShouldFail()
    {
        var userRepository = new Mock<IUserRepository>();
        userRepository
            .Setup(x => x.GetByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var handler = new UpdateUserStatusCommandHandler(
            userRepository.Object,
            CreateAdminRepository().Object,
            CreateAuditWriter().Object,
            CreateCurrentUser(ActorId).Object);

        var result = await handler.Handle(
            new UpdateUserStatusCommand(TargetId, IsActive: false),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(AdminErrors.UserNotFound.Code);
    }

    [Fact]
    public async Task UpdateStatus_WhenDeactivatingAnotherUser_ShouldDeactivateAndAudit()
    {
        var user = CreateUser(TargetId);
        var userRepository = new Mock<IUserRepository>();
        userRepository
            .Setup(x => x.GetByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var auditWriter = CreateAuditWriter();

        var handler = new UpdateUserStatusCommandHandler(
            userRepository.Object,
            CreateAdminRepository().Object,
            auditWriter.Object,
            CreateCurrentUser(ActorId).Object);

        var result = await handler.Handle(
            new UpdateUserStatusCommand(TargetId, IsActive: false),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.IsActive.Should().BeFalse();
        userRepository.Verify(
            x => x.UpdateAsync(user, It.IsAny<CancellationToken>()),
            Times.Once);
        auditWriter.Verify(
            x => x.WriteAsync(
                ActorId,
                AdminAuditActions.UserDeactivated,
                "User",
                TargetId.ToString(),
                It.IsAny<object?>(),
                It.IsAny<AuditSeverity>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
