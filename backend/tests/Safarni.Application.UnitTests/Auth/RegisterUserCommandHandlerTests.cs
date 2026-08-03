using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Abstractions.Security;
using Safarni.Application.Common.Errors;
using Safarni.Application.Features.Auth.Register;
using Safarni.Domain.Entities;
using Xunit;

namespace Safarni.Application.UnitTests.Auth;

public class RegisterUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IEmailVerificationLinkFactory> _linkFactory = new();
    private readonly Mock<IServiceScopeFactory> _scopeFactory = new();

    public RegisterUserCommandHandlerTests()
    {
        _passwordHasher.Setup(hasher => hasher.Hash(It.IsAny<string>()))
            .Returns((string p) => $"hashed-{p}");
        _linkFactory.Setup(factory => factory.Create(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((email, token) => $"https://safarni.local/verify?email={email}&token={token}");

        var emailService = new Mock<IEmailService>();
        emailService.Setup(service => service.SendEmailVerificationAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var provider = new Mock<IServiceProvider>();
        provider.Setup(service => service.GetService(typeof(IEmailService)))
            .Returns(emailService.Object);

        var scope = new Mock<IServiceScope>();
        scope.Setup(s => s.ServiceProvider).Returns(provider.Object);

        _scopeFactory.Setup(factory => factory.CreateScope()).Returns(scope.Object);
    }

    [Fact]
    public async Task Handle_NewEmail_AddsUserWithVerificationToken_AndReturnsSuccess()
    {
        var handler = new RegisterUserCommandHandler(
            _userRepository.Object,
            _passwordHasher.Object,
            _linkFactory.Object,
            _scopeFactory.Object);

        var command = new RegisterUserCommand(
            "Mona Ali",
            "Mona@Example.COM",
            "SecretPass!1",
            "SecretPass!1");

        var result = await handler.Handle(command, CancellationToken.None);

        var addedUser = _userRepository.Invocations
            .Single(invocation => invocation.Method.Name == nameof(IUserRepository.AddAsync))
            .Arguments[0] as User;
        string verificationToken = addedUser!.EmailVerificationToken!;

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(addedUser!.Id);
        result.Value.FullName.Should().Be("Mona Ali");
        addedUser.Email.Should().Be("mona@example.com");
        addedUser.IsEmailVerified.Should().BeFalse();
        addedUser.IsActive.Should().BeTrue();
        verificationToken.Should().HaveLength(64);
        addedUser.EmailVerificationTokenExpiresAt.Should().BeCloseTo(
            addedUser.CreatedAt.AddHours(24),
            precision: TimeSpan.FromSeconds(1));
        _passwordHasher.Verify(hasher => hasher.Hash("SecretPass!1"), Times.Once);
    }

    [Fact]
    public async Task HandleExistingEmail_ReturnsAlreadyExists_AndDoesNotAddUser()
    {
        _userRepository.Setup(repository => repository.EmailExistsAsync("used@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new RegisterUserCommandHandler(
            _userRepository.Object,
            _passwordHasher.Object,
            _linkFactory.Object,
            _scopeFactory.Object);

        var command = new RegisterUserCommand(
            "Ali",
            "USED@Example.COM",
            "SecretPass!2",
            "SecretPass!2");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(UserErrors.EmailAlreadyExists.Code);
        _userRepository.Verify(
            repository => repository.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleNewEmail_TrimsNormalizes_AndHashesPassword()
    {
        var handler = new RegisterUserCommandHandler(
            _userRepository.Object,
            _passwordHasher.Object,
            _linkFactory.Object,
            _scopeFactory.Object);

        var command = new RegisterUserCommand(
            "   Omar Khaled   ",
            "  Omar@Example.COM ",
            "SecretPass!3",
            "SecretPass!3");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var argument = _userRepository.Invocations
            .First(invocation => invocation.Method.Name == nameof(IUserRepository.AddAsync))
            .Arguments[0] as User;

        argument!.FullName.Should().Be("Omar Khaled");
        argument.Email.Should().Be("omar@example.com");
        argument.PasswordHash.Should().Be("hashed-SecretPass!3");
        _linkFactory.Verify(
            factory => factory.Create("omar@example.com", argument.EmailVerificationToken!),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Sets24HourTokenExpiry_AndAssignsLinkToToken()
    {
        var handler = new RegisterUserCommandHandler(
            _userRepository.Object,
            _passwordHasher.Object,
            _linkFactory.Object,
            _scopeFactory.Object);

        var command = new RegisterUserCommand(
            "Sara",
            "sara@example.com",
            "SecretPass1234",
            "SecretPass1234");

        await handler.Handle(command, CancellationToken.None);

        var user = _userRepository.Invocations
            .First(invocation => invocation.Method.Name == nameof(IUserRepository.AddAsync))
            .Arguments[0] as User;

        user!.EmailVerificationToken.Should().NotBeNullOrEmpty();
        user.EmailVerificationTokenExpiresAt.Should().Be(user.CreatedAt.AddHours(24));
        _linkFactory.Verify(factory => factory.Create("sara@example.com", user.EmailVerificationToken), Times.Once);
    }
}