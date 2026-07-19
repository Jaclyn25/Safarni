using MediatR;
using Safarni.Application.Common.Results;

namespace Safarni.Application.Features.Profile.GetMyProfile
{
    public sealed record GetMyProfileQuery(Guid UserId)
        : IRequest<Result<GetMyProfileResponse>>;
}