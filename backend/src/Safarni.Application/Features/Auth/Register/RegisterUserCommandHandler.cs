using MediatR;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Abstractions.Security;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;
using Safarni.Domain.Entities;
using System.Security.Cryptography;

namespace Safarni.Application.Features.Auth.Register;

public sealed class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Result<RegisterUserResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailService _emailService;

    public RegisterUserCommandHandler(IUserRepository userRepository, IPasswordHasher passwordHasher, IEmailService emailService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _emailService = emailService;
    }


    public async Task<Result<RegisterUserResponse>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        if (await _userRepository.EmailExistsAsync(normalizedEmail, cancellationToken))
        {
            return Result<RegisterUserResponse>.Failure(UserErrors.EmailAlreadyExists);
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName.Trim(),
            Email = normalizedEmail,
            PasswordHash = _passwordHasher.Hash(request.Password),
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            IsEmailVerified = false
        };

        var verificationToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        user.EmailVerificationToken = verificationToken;
        user.EmailVerificationTokenExpiresAt = DateTime.UtcNow.AddHours(24);

        await _userRepository.AddAsync(user, cancellationToken);

        var verifyLink = $"http://localhost:5110/api/Auth/verify-email?email={Uri.EscapeDataString(user.Email)}&token={Uri.EscapeDataString(verificationToken)}";

        try
        {
            await _emailService.SendEmailVerificationAsync(
                user.Email,
                verifyLink,
                cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine("========== EMAIL ERROR ==========");
            Console.WriteLine(ex.ToString());
            Console.WriteLine("=================================");
        }

        return Result<RegisterUserResponse>.Success(new RegisterUserResponse(
            user.Id,
            user.FullName,
            user.Email,
            user.CreatedAt,
            user.IsActive));
    }
}