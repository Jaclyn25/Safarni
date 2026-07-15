using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Safarni.Application.Abstractions.Security;
using Safarni.Application.Behaviors;
using Safarni.Application.Features.Auth.ChangePassword;
using Safarni.Application.Features.Auth.ForgotPassword;
using Safarni.Application.Features.Auth.Login;
using Safarni.Application.Features.Auth.Logout;
using Safarni.Application.Features.Auth.RefreshTokens;
using Safarni.Application.Features.Auth.Register;
using Safarni.Application.Features.Auth.VerifyEmail;
using Safarni.Application.Features.Auth.GoogleLogin;
using Safarni.Application.Features.Auth.ResetPassword;


namespace Safarni.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(typeof(DependencyInjection).Assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient<IValidator<RegisterUserCommand>, RegisterUserCommandValidator>();
        services.AddTransient<IValidator<LoginCommand>, LoginCommandValidator>();
        services.AddTransient<IValidator<RefreshTokenCommand>, RefreshTokenCommandValidator>();
        services.AddTransient<IValidator<LogoutCommand>, LogoutCommandValidator>();
        services.AddTransient<IValidator<ForgotPasswordCommand>, ForgotPasswordCommandValidator>();
        services.AddTransient<IValidator<VerifyEmailCommand>, VerifyEmailCommandValidator>();
        services.AddTransient<IValidator<ChangePasswordCommand>, ChangePasswordCommandValidator>();
        services.AddTransient<IValidator<ResetPasswordCommand>, ResetPasswordCommandValidator>();
        services.AddTransient<IValidator<GoogleLoginCommand>, GoogleLoginCommandValidator>();

        

        return services;
    }
}
