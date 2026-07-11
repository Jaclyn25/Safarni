using MediatR;
using Safarni.Application.Common.Results;

namespace Safarni.Application.Features.Auth.Login;

public sealed record LoginCommand(string Email, string Password) : IRequest<Result<LoginResponse>>;