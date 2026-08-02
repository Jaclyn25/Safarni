using MediatR;
using Safarni.Application.Abstractions.Auditing;
using Safarni.Application.Abstractions.Authentication;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Results;
using Safarni.Application.Features.Admin.Common;
using Safarni.Application.Features.Admin.Users;

namespace Safarni.Application.Features.Admin.Users;

public sealed record UpdateUserStatusCommand(Guid UserId, bool IsActive)
    : IRequest<Result<AdminUserDetailResponse>>;

public sealed class UpdateUserStatusCommandHandler
    : IRequestHandler<UpdateUserStatusCommand, Result<AdminUserDetailResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IAdminRepository _adminRepository;
    private readonly IAuditLogWriter _auditLogWriter;
    private readonly ICurrentUserService _currentUser;

    public UpdateUserStatusCommandHandler(
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
        UpdateUserStatusCommand request,
        CancellationToken cancellationToken)
    {
        var actorId = _currentUser.UserId;

        if (!request.IsActive && actorId == request.UserId)
        {
            return Result<AdminUserDetailResponse>.Failure(
                AdminErrors.SelfDeactivationNotAllowed);
        }

        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return Result<AdminUserDetailResponse>.Failure(AdminErrors.UserNotFound);
        }

        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user, cancellationToken);

        var action = request.IsActive
            ? AdminAuditActions.UserActivated
            : AdminAuditActions.UserDeactivated;

        await _auditLogWriter.WriteAsync(
            actorId,
            action,
            "User",
            request.UserId.ToString(),
            new { previousState = !request.IsActive, newState = request.IsActive },
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
