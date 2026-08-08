using System.Security.Cryptography;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Abstractions.Security;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;
using Safarni.Domain.Entities;
using Safarni.Domain.Enums;

namespace Safarni.Application.Features.Auth.Register;

public sealed class RegisterUserCommandHandler
    : IRequestHandler<RegisterUserCommand, Result<RegisterUserResponse>>
{
    private static readonly TimeSpan VerificationTokenLifetime = TimeSpan.FromHours(24);

    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailVerificationLinkFactory _emailVerificationLinkFactory;
    private readonly IServiceScopeFactory _scopeFactory;

    public RegisterUserCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IEmailVerificationLinkFactory emailVerificationLinkFactory,
        IServiceScopeFactory scopeFactory)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _emailVerificationLinkFactory = emailVerificationLinkFactory;
        _scopeFactory = scopeFactory;
    }

    public async Task<Result<RegisterUserResponse>> Handle(
        RegisterUserCommand request,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        if (await _userRepository.EmailExistsAsync(normalizedEmail, cancellationToken))
        {
            return Result<RegisterUserResponse>.Failure(UserErrors.EmailAlreadyExists);
        }

        var agencyDetails = request.Agency;
        var isAgencyRegistration =
            request.AccountType == RegistrationAccountType.TravelAgency;
        var normalizedCompanyName = isAgencyRegistration && agencyDetails is not null
            ? NormalizeCompanyName(agencyDetails.CompanyName)
            : null;

        if (isAgencyRegistration && agencyDetails is null)
        {
            return Result<RegisterUserResponse>.Failure(
                TravelAgencyErrors.RegistrationDetailsRequired);
        }

        if (normalizedCompanyName is not null &&
            await _userRepository.TravelAgencyCompanyNameExistsAsync(
                normalizedCompanyName,
                cancellationToken))
        {
            return Result<RegisterUserResponse>.Failure(
                TravelAgencyErrors.CompanyNameAlreadyExists);
        }

        var verificationToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var createdAt = DateTime.UtcNow;

        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName.Trim(),
            Email = normalizedEmail,
            Role = isAgencyRegistration
                ? UserRole.TravelAgency
                : UserRole.User,
            PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber)
                ? null
                : request.PhoneNumber.Trim(),
            PasswordHash = _passwordHasher.Hash(request.Password),
            CreatedAt = createdAt,
            IsActive = true,
            IsEmailVerified = false,
            EmailVerificationToken = verificationToken,
            EmailVerificationTokenExpiresAt = createdAt.Add(VerificationTokenLifetime)
        };

        TravelAgency? travelAgency = null;

        if (isAgencyRegistration)
        {
            travelAgency = new TravelAgency
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                CompanyName = normalizedCompanyName!,
                Description = agencyDetails!.Description.Trim(),
                Address = agencyDetails.Address.Trim(),
                City = agencyDetails.City.Trim(),
                Country = agencyDetails.Country.Trim(),
                CompanyPhone = agencyDetails.CompanyPhone.Trim(),
                LogoUrl = TrimToNull(agencyDetails.LogoUrl),
                WebsiteUrl = TrimToNull(agencyDetails.WebsiteUrl),
                VerificationStatus = AgencyVerificationStatus.Pending,
                CreatedAt = createdAt
            };

            var persistenceResult =
                await _userRepository.TryAddTravelAgencyAsync(
                    user,
                    travelAgency,
                    cancellationToken);

            if (persistenceResult ==
                AgencyRegistrationPersistenceResult.DuplicateEmail)
            {
                return Result<RegisterUserResponse>.Failure(
                    UserErrors.EmailAlreadyExists);
            }

            if (persistenceResult ==
                AgencyRegistrationPersistenceResult.DuplicateCompanyName)
            {
                return Result<RegisterUserResponse>.Failure(
                    TravelAgencyErrors.CompanyNameAlreadyExists);
            }
        }
        else
        {
            await _userRepository.AddAsync(user, cancellationToken);
        }

        var verificationLink = _emailVerificationLinkFactory.Create(
            user.Email,
            verificationToken);

        QueueVerificationEmail(user.Email, verificationLink);

        return Result<RegisterUserResponse>.Success(
            new RegisterUserResponse(
                user.Id,
                user.FullName,
                user.Email,
                user.CreatedAt,
                user.IsActive,
                request.AccountType,
                travelAgency is null
                    ? null
                    : new RegisteredTravelAgencyResponse(
                        travelAgency.Id,
                        travelAgency.CompanyName,
                        travelAgency.VerificationStatus)));
    }

    private static string NormalizeCompanyName(string companyName)
    {
        return string.Join(
            ' ',
            companyName.Split(
                (char[]?)null,
                StringSplitOptions.RemoveEmptyEntries));
    }

    private static string? TrimToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private void QueueVerificationEmail(string email, string verificationLink)
    {
        // This preserves the project's current non-blocking registration behavior.
        // For production, replace this with a durable queue/outbox worker.
        _ = Task.Run(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            try
            {
                await emailService.SendEmailVerificationAsync(
                    email,
                    verificationLink,
                    CancellationToken.None);
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(
                    $"Failed to send verification email to {email}: {exception}");
            }
        });
    }
}
