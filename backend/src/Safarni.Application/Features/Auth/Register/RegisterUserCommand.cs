using MediatR;
using Safarni.Application.Common.Results;

namespace Safarni.Application.Features.Auth.Register;

public sealed record RegisterUserCommand(
    string FullName,
    string Email,
    string Password,
    string ConfirmPassword) : IRequest<Result<RegisterUserResponse>>;