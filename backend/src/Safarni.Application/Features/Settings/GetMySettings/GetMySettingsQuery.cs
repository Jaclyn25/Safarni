using MediatR;
using Safarni.Application.Common.Results;

namespace Safarni.Application.Features.Settings.GetMySettings;

public sealed record GetMySettingsQuery(Guid UserId) : IRequest<Result<GetMySettingsResponse>>;
