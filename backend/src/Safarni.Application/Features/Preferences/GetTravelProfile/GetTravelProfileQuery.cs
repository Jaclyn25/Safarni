using MediatR;
using Safarni.Application.Common.Results;

namespace Safarni.Application.Features.Preferences.GetTravelProfile;

public sealed record GetTravelProfileQuery : IRequest<Result<TravelProfileResponse>>;
