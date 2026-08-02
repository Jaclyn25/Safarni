using MediatR;
using Safarni.Application.Abstractions.Auditing;
using Safarni.Application.Abstractions.Authentication;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Results;
using Safarni.Application.Features.Admin.Common;
using Safarni.Application.Features.Admin.Users;

namespace Safarni.Application.Features.Admin.Users;

public sealed record VerifyUserEmailCommand(Guid UserId)
    : IRequest<Result<AdminUserDetailResponse>>;

public sealed class VerifyUserEmailCommandHandler
    : IRequestHandler<VerifyUserEmailCommand, Result<AdminUserDetailResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IAdminRepository _adminRepository;
    private readonly IAuditLogWriter _auditLogWriter;
    private readonly ICurrentUserService _currentUser;

    public VerifyUserEmailCommandHandler(
        IUserRepository userRepository,
        IAdminRepository adminRepository,
        IAuditLogWriter auditLogWriter,
        ICurrentUserService currentUser)
    {
        _userRepository = userRepository;
        _adminRepository = adminRepository;
        _auditLogWriter = auditLogWriter;
        _currentUser = currentUser;
    }

    public async Task<Result<AdminUserDetailResponse>> Handle(
        VerifyUserEmailCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return Result<AdminUserDetailResponse>.Failure(AdminErrors.UserNotFound);
        }

        if (!user.IsEmailVerified)
        {
            user.IsEmailVerified = true;
            user.EmailVerificationToken = null;
            user.EmailVerificationTokenExpiresAt = null;
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user, cancellationToken);
        }

        await _auditLogWriter.WriteAsync(
            _currentUser.UserId,
            AdminAuditActions.UserEmailVerifiedByAdmin,
            "User",
            request.UserId.ToString(),
            cancellationToken: cancellationToken);

        var counts = await _adminRepository.GetUserCountsAsync(
            request.UserId,
            cancellationToken);

        return Result<AdminUserDetailResponse>.Success(
            AdminUserResponseMapper.ToDetail(
                user,
                counts.Trips,
                counts.VisaApplications,
                counts.Conversations,
                counts.Messages));
    }
}
