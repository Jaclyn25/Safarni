using MediatR;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;

namespace Safarni.Application.Features.Auth.VerifyEmail;

public sealed class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand, Result<VerifyEmailResponse>>
{
    private readonly IUserRepository _userRepository;

    public VerifyEmailCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<VerifyEmailResponse>> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);

        if (user is null)
        {
            return Result<VerifyEmailResponse>.Failure(new Error("Auth.UserNotFound", "User not found.", ErrorType.NotFound));
        }

        if (user.IsEmailVerified)
        {
            return Result<VerifyEmailResponse>.Success(new VerifyEmailResponse("Email already verified."));
        }

        if (user.EmailVerificationToken != request.Token)
        {
            return Result<VerifyEmailResponse>.Failure(new Error("Auth.InvalidVerificationToken", "Invalid verification token.", ErrorType.Unauthorized));
        }

        if (user.EmailVerificationTokenExpiresAt is null || user.EmailVerificationTokenExpiresAt < DateTime.UtcNow)
        {
            return Result<VerifyEmailResponse>.Failure(new Error("Auth.ExpiredVerificationToken", "Verification token expired.", ErrorType.Unauthorized));
        }

        user.IsEmailVerified = true;
        user.EmailVerificationToken = null;
        user.EmailVerificationTokenExpiresAt = null;

        await _userRepository.UpdateAsync(user, cancellationToken);

        return Result<VerifyEmailResponse>.Success(new VerifyEmailResponse("Email verified successfully."));
    }
}