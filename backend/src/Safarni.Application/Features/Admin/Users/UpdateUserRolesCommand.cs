using MediatR;
using Safarni.Application.Abstractions.Auditing;
using Safarni.Application.Abstractions.Authentication;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Results;
using Safarni.Application.Features.Admin.Common;
using Safarni.Application.Features.Admin.Users;
using Safarni.Domain.Enums;

namespace Safarni.Application.Features.Admin.Users;

public sealed record UpdateUserRolesCommand(Guid UserId, UserRole Role)
    : IRequest<Result<AdminUserDetailResponse>>;

public sealed class UpdateUserRolesCommandHandler
    : IRequestHandler<UpdateUserRolesCommand, Result<AdminUserDetailResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IAdminRepository _adminRepository;
    private readonly IAuditLogWriter _auditLogWriter;
    private readonly ICurrentUserService _currentUser;

    public UpdateUserRolesCommandHandler(
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
        UpdateUserRolesCommand request,
        CancellationToken cancellationToken)
    {
        var actorId = _currentUser.UserId;

        if (actorId == request.UserId && request.Role != UserRole.Admin)
        {
            return Result<AdminUserDetailResponse>.Failure(
                AdminErrors.SelfRoleChangeNotAllowed);
        }

        if (!Enum.IsDefined(request.Role))
        {
            return Result<AdminUserDetailResponse>.Failure(AdminErrors.InvalidRole);
        }

        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return Result<AdminUserDetailResponse>.Failure(AdminErrors.UserNotFound);
        }

        var previousRole = user.Role;
        if (previousRole != request.Role &&
            (previousRole == UserRole.TravelAgency ||
             request.Role == UserRole.TravelAgency))
        {
            return Result<AdminUserDetailResponse>.Failure(
                AdminErrors.AgencyRoleManagedByAgencyWorkflow);
        }

        if (previousRole != request.Role)
        {
            user.Role = request.Role;
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user, cancellationToken);
        }

        await _auditLogWriter.WriteAsync(
            actorId,
            AdminAuditActions.UserRoleChanged,
            "User",
            request.UserId.ToString(),
            new { previousRole = previousRole.ToString(), newRole = request.Role.ToString() },
            cancellationToken: cancellationToken);

        var counts = await _adminRepository.GetUserCountsAsync(
            request.UserId,
            cancellationToken);

        return Result<AdminUserDetailResponse>.Success(
            AdminUserResponseMapper.ToDetail(
                user,
                counts.Trips,
                counts.Conversations,
                counts.Messages));
    }
}
