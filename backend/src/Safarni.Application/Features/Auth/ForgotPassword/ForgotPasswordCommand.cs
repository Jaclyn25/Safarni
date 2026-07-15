using System;
using System.Collections.Generic;
using System.Text;
using MediatR;
using Safarni.Application.Common.Results;

namespace Safarni.Application.Features.Auth.ForgotPassword;

public sealed record ForgotPasswordCommand(string Email) : IRequest<Result<ForgotPasswordResponse>>;