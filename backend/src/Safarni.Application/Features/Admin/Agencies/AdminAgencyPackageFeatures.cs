using MediatR;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;
using Safarni.Application.Features.Packages;

namespace Safarni.Application.Features.Admin.Agencies;

public sealed record ListAdminAgencyPackagesQuery(Guid AgencyId)
    : IRequest<Result<IReadOnlyList<TravelPackageResponse>>>;

public sealed class ListAdminAgencyPackagesQueryHandler
    : IRequestHandler<ListAdminAgencyPackagesQuery, Result<IReadOnlyList<TravelPackageResponse>>>
{
    private readonly ITravelAgencyRepository _agencies;
    private readonly ITravelPackageRepository _packages;
    public ListAdminAgencyPackagesQueryHandler(ITravelAgencyRepository agencies, ITravelPackageRepository packages)
    {
        _agencies = agencies;
        _packages = packages;
    }

    public async Task<Result<IReadOnlyList<TravelPackageResponse>>> Handle(ListAdminAgencyPackagesQuery request, CancellationToken cancellationToken)
    {
        if (await _agencies.GetByIdAsync(request.AgencyId, cancellationToken: cancellationToken) is null)
            return Result<IReadOnlyList<TravelPackageResponse>>.Failure(TravelAgencyErrors.NotFound);
        var packages = await _packages.ListByAgencyAsync(request.AgencyId, cancellationToken);
        return Result<IReadOnlyList<TravelPackageResponse>>.Success(packages.Select(TravelPackageMapping.ToResponse).ToArray());
    }
}
