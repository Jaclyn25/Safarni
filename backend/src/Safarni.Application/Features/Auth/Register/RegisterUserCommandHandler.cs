using MediatR;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Abstractions.Security;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;
using Safarni.Domain.Entities;
<<<<<<< Updated upstream
=======
using System.Security.Cryptography;
using Microsoft.Extensions.DependencyInjection;
>>>>>>> Stashed changes

namespace Safarni.Application.Features.Auth.Register;

public sealed class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Result<RegisterUserResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
<<<<<<< Updated upstream

    public RegisterUserCommandHandler(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
=======
    private readonly IServiceProvider _serviceProvider;

    public RegisterUserCommandHandler(IUserRepository userRepository, IPasswordHasher passwordHasher, IServiceProvider serviceProvider)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _serviceProvider = serviceProvider;
>>>>>>> Stashed changes
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
            IsActive = true
        };

        await _userRepository.AddAsync(user, cancellationToken);

<<<<<<< Updated upstream
=======
        var verifyLink = $"http://localhost:5110/api/Auth/verify-email?email={Uri.EscapeDataString(user.Email)}&token={Uri.EscapeDataString(verificationToken)}";

        _ = Task.Run(async () =>
        {
            using var scope = _serviceProvider.CreateScope();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            try
            {
                await emailService.SendEmailVerificationAsync(
                    user.Email,
                    verifyLink,
                    CancellationToken.None);
            }
            catch (Exception ex)
            {
                Console.WriteLine("========== EMAIL ERROR ==========");
                Console.WriteLine(ex.ToString());
                Console.WriteLine("=================================");
            }
        });

>>>>>>> Stashed changes
        return Result<RegisterUserResponse>.Success(new RegisterUserResponse(
            user.Id,
            user.FullName,
            user.Email,
            user.CreatedAt,
            user.IsActive));
    }
}