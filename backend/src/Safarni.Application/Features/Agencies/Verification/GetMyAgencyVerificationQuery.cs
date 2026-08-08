using MediatR;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;
using Safarni.Application.Features.Agencies.Common;

namespace Safarni.Application.Features.Agencies.Verification;

public sealed record GetMyAgencyVerificationQuery(Guid UserId)
    : IRequest<Result<AgencyVerificationStatusResponse>>;

public sealed class GetMyAgencyVerificationQueryHandler
    : IRequestHandler<GetMyAgencyVerificationQuery, Result<AgencyVerificationStatusResponse>>
{
    private readonly ITravelAgencyRepository _agencies;

    public GetMyAgencyVerificationQueryHandler(ITravelAgencyRepository agencies)
    {
        _agencies = agencies;
    }

    public async Task<Result<AgencyVerificationStatusResponse>> Handle(
        GetMyAgencyVerificationQuery request,
        CancellationToken cancellationToken)
    {
        var agency = await _agencies.GetByUserIdAsync(
            request.UserId,
            includeDocuments: true,
            cancellationToken);

        return agency is null
            ? Result<AgencyVerificationStatusResponse>.Failure(TravelAgencyErrors.NotFound)
            : Result<AgencyVerificationStatusResponse>.Success(
                AgencyResponseMapper.ToVerificationStatus(agency));
    }
}
