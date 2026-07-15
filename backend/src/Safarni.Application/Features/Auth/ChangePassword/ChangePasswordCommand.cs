using MediatR;
using Safarni.Application.Common.Results;

namespace Safarni.Application.Features.Auth.ChangePassword;

public sealed record ChangePasswordCommand(
    Guid UserId,
    string CurrentPassword,
    string NewPassword,
    string ConfirmPassword) : IRequest<Result<ChangePasswordResponse>>;