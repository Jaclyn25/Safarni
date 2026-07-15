using MediatR;
using Safarni.Application.Common.Results;

namespace Safarni.Application.Features.Auth.VerifyEmail;

public sealed record VerifyEmailCommand(string Email, string Token) : IRequest<Result<VerifyEmailResponse>>;