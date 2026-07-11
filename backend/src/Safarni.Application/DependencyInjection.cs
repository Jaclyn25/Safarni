using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Safarni.Application.Behaviors;
using Safarni.Application.Features.Auth.Login;
using Safarni.Application.Features.Auth.Logout;
using Safarni.Application.Features.Auth.RefreshTokens;
using Safarni.Application.Features.Auth.Register;

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

        return services;
    }
}
