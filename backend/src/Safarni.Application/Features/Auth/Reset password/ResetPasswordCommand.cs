using MediatR;
using Safarni.Application.Common.Results;

namespace Safarni.Application.Features.Auth.ResetPassword;

public sealed record ResetPasswordCommand(string Email, string Token, string NewPassword, string ConfirmPassword) : IRequest<Result<ResetPasswordResponse>>;