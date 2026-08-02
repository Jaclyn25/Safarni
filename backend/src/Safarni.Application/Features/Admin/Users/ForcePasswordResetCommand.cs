using MediatR;
using Microsoft.Extensions.Configuration;
using Safarni.Application.Abstractions.Auditing;
using Safarni.Application.Abstractions.Authentication;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Abstractions.Security;
using Safarni.Application.Common.Results;
using Safarni.Application.Features.Admin.Common;

namespace Safarni.Application.Features.Admin.Users;

public sealed record ForcePasswordResetCommand(Guid UserId)
    : IRequest<Result<ForcePasswordResetResponse>>;

public sealed class ForcePasswordResetCommandHandler
    : IRequestHandler<ForcePasswordResetCommand, Result<ForcePasswordResetResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IAuditLogWriter _auditLogWriter;
    private readonly ICurrentUserService _currentUser;
    private readonly IPasswordResetLinkFactory _passwordResetLinkFactory;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public ForcePasswordResetCommandHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IAuditLogWriter auditLogWriter,
        ICurrentUserService currentUser,
        IPasswordResetLinkFactory passwordResetLinkFactory,
        IEmailService emailService,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _auditLogWriter = auditLogWriter;
        _currentUser = currentUser;
        _passwordResetLinkFactory = passwordResetLinkFactory;
        _emailService = emailService;
        _configuration = configuration;
    }

    public async Task<Result<ForcePasswordResetResponse>> Handle(
        ForcePasswordResetCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return Result<ForcePasswordResetResponse>.Failure(AdminErrors.UserNotFound);
        }

        var token = Guid.NewGuid().ToString("N");
        user.PasswordResetToken = token;
        user.PasswordResetTokenExpiresAt = DateTime.UtcNow.AddHours(1);
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user, cancellationToken);

        await _refreshTokenRepository.RevokeAllByUserIdAsync(request.UserId, cancellationToken);

        var emailConfigured = IsEmailConfigured();
        if (emailConfigured)
        {
            var resetLink = _passwordResetLinkFactory.Create(user.Email, token);
            await _emailService.SendPasswordResetAsync(
                user.Email,
                resetLink,
                cancellationToken);
        }

        await _auditLogWriter.WriteAsync(
            _currentUser.UserId,
            AdminAuditActions.UserPasswordResetForced,
            "User",
            request.UserId.ToString(),
            new { emailSent = emailConfigured },
            cancellationToken: cancellationToken);

        return Result<ForcePasswordResetResponse>.Success(
            new ForcePasswordResetResponse(
                emailConfigured,
                emailConfigured
                    ? "Sessions revoked and a password reset link was sent to the user's email."
                    : "Sessions revoked and a password reset token was issued. Email is not configured, so no email was sent."));
    }

    private bool IsEmailConfigured()
    {
        var from = _configuration["EmailSettings:From"];
        var userName = _configuration["EmailSettings:UserName"];
        var password = _configuration["EmailSettings:Password"];

        return !string.IsNullOrWhiteSpace(from) &&
            !string.IsNullOrWhiteSpace(userName) &&
            !string.IsNullOrWhiteSpace(password);
    }
}
