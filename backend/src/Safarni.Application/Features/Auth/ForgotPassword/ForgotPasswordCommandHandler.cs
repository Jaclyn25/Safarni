using MediatR;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Abstractions.Security;
using Safarni.Application.Common.Results;

namespace Safarni.Application.Features.Auth.ForgotPassword;

public sealed class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Result<ForgotPasswordResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;
    private readonly IPasswordResetLinkFactory _passwordResetLinkFactory;

    public ForgotPasswordCommandHandler(
        IUserRepository userRepository,
        IEmailService emailService,
        IPasswordResetLinkFactory passwordResetLinkFactory)
    {
        _userRepository = userRepository;
        _emailService = emailService;
        _passwordResetLinkFactory = passwordResetLinkFactory;
    }

    public async Task<Result<ForgotPasswordResponse>> Handle(
        ForgotPasswordCommand request,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);

        // Do not reveal whether an account exists for this email address.
        if (user is null)
        {
            return Result<ForgotPasswordResponse>.Success(
                new ForgotPasswordResponse("If the email exists, a reset link has been sent."));
        }

        var token = Guid.NewGuid().ToString("N");
        user.PasswordResetToken = token;
        user.PasswordResetTokenExpiresAt = DateTime.UtcNow.AddHours(1);

        await _userRepository.UpdateAsync(user, cancellationToken);

        var resetLink = _passwordResetLinkFactory.Create(user.Email, token);
        await _emailService.SendPasswordResetAsync(user.Email, resetLink, cancellationToken);

        return Result<ForgotPasswordResponse>.Success(
            new ForgotPasswordResponse("If the email exists, a reset link has been sent."));
    }
}
