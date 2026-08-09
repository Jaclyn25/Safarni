using MediatR;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Results;
using Safarni.Application.Features.Admin.Common;

namespace Safarni.Application.Features.Admin.Users;

public sealed record GetAdminUserQuery(Guid UserId)
    : IRequest<Result<AdminUserDetailResponse>>;

public sealed class GetAdminUserQueryHandler
    : IRequestHandler<GetAdminUserQuery, Result<AdminUserDetailResponse>>
{
    private readonly IAdminRepository _adminRepository;
    private readonly IUserRepository _userRepository;

    public GetAdminUserQueryHandler(
        IAdminRepository adminRepository,
        IUserRepository userRepository)
    {
        _adminRepository = adminRepository;
        _userRepository = userRepository;
    }

    public async Task<Result<AdminUserDetailResponse>> Handle(
        GetAdminUserQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return Result<AdminUserDetailResponse>.Failure(AdminErrors.UserNotFound);
        }

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
