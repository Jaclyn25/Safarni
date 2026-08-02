using MediatR;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Results;
using Safarni.Application.Features.Admin.Common;

namespace Safarni.Application.Features.Admin.Users;

public sealed record ListAdminUsersQuery(UserSearchCriteria Criteria)
    : IRequest<Result<PagedResult<AdminUserSummaryResponse>>>;

public sealed class ListAdminUsersQueryHandler
    : IRequestHandler<ListAdminUsersQuery, Result<PagedResult<AdminUserSummaryResponse>>>
{
    private readonly IAdminRepository _adminRepository;

    public ListAdminUsersQueryHandler(IAdminRepository adminRepository)
    {
        _adminRepository = adminRepository;
    }

    public async Task<Result<PagedResult<AdminUserSummaryResponse>>> Handle(
        ListAdminUsersQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _adminRepository.SearchUsersAsync(
            request.Criteria,
            cancellationToken);

        return Result<PagedResult<AdminUserSummaryResponse>>.Success(
            new PagedResult<AdminUserSummaryResponse>(
                result.Items
                    .Select(AdminUserResponseMapper.ToSummary)
                    .ToArray(),
                result.Page,
                result.PageSize,
                result.TotalCount));
    }
}
