using MediatR;
using Safarni.Application.Common.Results;
using Safarni.Application.Features.Auth.Login;

namespace Safarni.Application.Features.Auth.GoogleLogin;

public sealed record GoogleLoginCommand(string IdToken) : IRequest<Result<LoginResponse>>;