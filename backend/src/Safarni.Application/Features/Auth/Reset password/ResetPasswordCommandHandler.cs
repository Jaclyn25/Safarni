using MediatR;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Abstractions.Security;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;

namespace Safarni.Application.Features.Auth.ResetPassword;

public sealed class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result<ResetPasswordResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public ResetPasswordCommandHandler(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<ResetPasswordResponse>> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);

        if (user is null)
        {
            return Result<ResetPasswordResponse>.Failure(new Error("Auth.UserNotFound", "User not found.", ErrorType.NotFound));
        }

        if (user.PasswordResetToken != request.Token)
        {
            return Result<ResetPasswordResponse>.Failure(new Error("Auth.InvalidResetToken", "Invalid reset token.", ErrorType.Unauthorized));
        }

        if (user.PasswordResetTokenExpiresAt is null || user.PasswordResetTokenExpiresAt < DateTime.UtcNow)
        {
            return Result<ResetPasswordResponse>.Failure(new Error("Auth.ExpiredResetToken", "Reset token expired.", ErrorType.Unauthorized));
        }

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiresAt = null;

        await _userRepository.UpdateAsync(user, cancellationToken);

        return Result<ResetPasswordResponse>.Success(new ResetPasswordResponse("Password has been reset."));
    }
}