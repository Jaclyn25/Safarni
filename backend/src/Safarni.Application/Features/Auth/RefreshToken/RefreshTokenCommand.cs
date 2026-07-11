using MediatR;
using Safarni.Application.Common.Results;

namespace Safarni.Application.Features.Auth.RefreshTokens;

public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<Result<RefreshTokenResponse>>;
