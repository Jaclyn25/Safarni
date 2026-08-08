using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Abstractions.Security;
using Safarni.Application.Common.Errors;
using Safarni.Application.Features.Auth.Register;
using Safarni.Domain.Entities;
using Safarni.Domain.Enums;
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
        _userRepository
            .Setup(repository => repository.TryAddTravelAgencyAsync(
                It.IsAny<User>(),
                It.IsAny<TravelAgency>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(AgencyRegistrationPersistenceResult.Created);

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

    [Fact]
    public void RegistrationContract_DefaultsToTraveler_AndAcceptsKnownAccountTypes()
    {
        var traveler = new RegisterUserCommand(
            "Traveler",
            "traveler@example.com",
            "SecretPass!4",
            "SecretPass!4");
        var agency = traveler with
        {
            AccountType = RegistrationAccountType.TravelAgency,
            PhoneNumber = "+201001234567",
            Agency = CreateAgencyDetails()
        };
        var validator = new RegisterUserCommandValidator();

        traveler.AccountType.Should().Be(RegistrationAccountType.Traveler);
        validator.Validate(traveler).IsValid.Should().BeTrue();
        validator.Validate(agency).IsValid.Should().BeTrue();
    }

    [Fact]
    public void RegistrationContract_TravelAgencyRequiresPhoneAndCompanyDetails()
    {
        var command = new RegisterUserCommand(
            "Agency Owner",
            "owner@agency.example",
            "SecretPass!5",
            "SecretPass!5",
            RegistrationAccountType.TravelAgency);

        var result = new RegisterUserCommandValidator().Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Select(error => error.PropertyName)
            .Should()
            .Contain(["PhoneNumber", "Agency"]);
    }

    [Fact]
    public async Task Handle_TravelAgencyChoice_CreatesLinkedPendingAgencyAtomically()
    {
        var handler = new RegisterUserCommandHandler(
            _userRepository.Object,
            _passwordHasher.Object,
            _linkFactory.Object,
            _scopeFactory.Object);
        var command = new RegisterUserCommand(
            "Agency Owner",
            "owner@agency.example",
            "SecretPass!5",
            "SecretPass!5",
            RegistrationAccountType.TravelAgency,
            "+201001234567",
            CreateAgencyDetails(
                companyName: "  Nile   Horizon Travel  "));

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AccountType.Should().Be(
            RegistrationAccountType.TravelAgency);
        result.Value.Agency.Should().NotBeNull();
        result.Value.Agency!.CompanyName.Should().Be(
            "Nile Horizon Travel");
        result.Value.Agency.VerificationStatus.Should().Be(
            AgencyVerificationStatus.Pending);

        _userRepository.Verify(
            repository => repository.TryAddTravelAgencyAsync(
                It.Is<User>(user =>
                    user.Role == UserRole.TravelAgency &&
                    user.PhoneNumber == "+201001234567"),
                It.Is<TravelAgency>(agency =>
                    agency.UserId != Guid.Empty &&
                    agency.CompanyName == "Nile Horizon Travel" &&
                    agency.VerificationStatus ==
                        AgencyVerificationStatus.Pending),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _userRepository.Verify(
            repository => repository.AddAsync(
                It.IsAny<User>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_DuplicateAgencyCompany_ReturnsConflictWithoutSaving()
    {
        _userRepository
            .Setup(repository =>
                repository.TravelAgencyCompanyNameExistsAsync(
                    "Nile Horizon Travel",
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var handler = new RegisterUserCommandHandler(
            _userRepository.Object,
            _passwordHasher.Object,
            _linkFactory.Object,
            _scopeFactory.Object);
        var command = new RegisterUserCommand(
            "Agency Owner",
            "second@agency.example",
            "SecretPass!6",
            "SecretPass!6",
            RegistrationAccountType.TravelAgency,
            "+201001234567",
            CreateAgencyDetails());

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(
            TravelAgencyErrors.CompanyNameAlreadyExists);
        _userRepository.Verify(
            repository => repository.TryAddTravelAgencyAsync(
                It.IsAny<User>(),
                It.IsAny<TravelAgency>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ConcurrentDuplicateCompany_ReturnsConflict()
    {
        _userRepository
            .Setup(repository => repository.TryAddTravelAgencyAsync(
                It.IsAny<User>(),
                It.IsAny<TravelAgency>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                AgencyRegistrationPersistenceResult.DuplicateCompanyName);
        var handler = new RegisterUserCommandHandler(
            _userRepository.Object,
            _passwordHasher.Object,
            _linkFactory.Object,
            _scopeFactory.Object);
        var command = new RegisterUserCommand(
            "Agency Owner",
            "racing@agency.example",
            "SecretPass!7",
            "SecretPass!7",
            RegistrationAccountType.TravelAgency,
            "+201001234567",
            CreateAgencyDetails());

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(
            TravelAgencyErrors.CompanyNameAlreadyExists);
    }

    private static TravelAgencyRegistrationDetails CreateAgencyDetails(
        string companyName = "Nile Horizon Travel")
    {
        return new TravelAgencyRegistrationDetails(
            companyName,
            "Organized cultural and leisure tours.",
            "12 Nile Street",
            "Cairo",
            "Egypt",
            "+20212345678",
            "https://cdn.example.com/agency-logo.png",
            "https://agency.example.com");
    }
}
