using MediatR;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Results;

namespace Safarni.Application.Features.Admin.Dashboard;

public sealed record GetRecentActivityQuery(int Limit = 20)
    : IRequest<Result<IReadOnlyList<RecentActivityResponse>>>;

public sealed record RecentActivityResponse(
    string Type,
    string Title,
    string? Detail,
    Guid? ActorUserId,
    string? ActorName,
    DateTime Timestamp);

public sealed class GetRecentActivityQueryHandler
    : IRequestHandler<GetRecentActivityQuery, Result<IReadOnlyList<RecentActivityResponse>>>
{
    private readonly IAdminRepository _adminRepository;

    public GetRecentActivityQueryHandler(IAdminRepository adminRepository)
    {
        _adminRepository = adminRepository;
    }

    public async Task<Result<IReadOnlyList<RecentActivityResponse>>> Handle(
        GetRecentActivityQuery request,
        CancellationToken cancellationToken)
    {
        var limit = Math.Clamp(request.Limit, 1, 100);
        var items = await _adminRepository.GetRecentActivityAsync(limit, cancellationToken);

        return Result<IReadOnlyList<RecentActivityResponse>>.Success(
            items
                .Select(item => new RecentActivityResponse(
                    item.Type,
                    item.Title,
                    item.Detail,
                    item.ActorUserId,
                    item.ActorName,
                    item.Timestamp))
                .ToArray());
    }
}
