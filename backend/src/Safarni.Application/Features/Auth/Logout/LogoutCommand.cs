using MediatR;
using Safarni.Application.Common.Results;

namespace Safarni.Application.Features.Auth.Logout;

public sealed record LogoutCommand(string RefreshToken) : IRequest<Result<LogoutResponse>>;
