using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Safarni.Application.Behaviors;
using Safarni.Application.Features.Auth.Login;
using Safarni.Application.Features.Auth.Logout;
using Safarni.Application.Features.Auth.RefreshTokens;
using Safarni.Application.Features.Auth.Register;
using Safarni.Application.Features.Preferences.UpsertTravelProfile;
using Safarni.Application.Features.VisaApplications.CreateVisaApplication;
using Safarni.Application.Features.VisaApplications.SelectVisaType;

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
        services.AddTransient<IValidator<UpsertTravelProfileCommand>, UpsertTravelProfileCommandValidator>();
        services.AddTransient<IValidator<CreateVisaApplicationCommand>, CreateVisaApplicationCommandValidator>();
        services.AddTransient<IValidator<SelectVisaTypeCommand>, SelectVisaTypeCommandValidator>();

        return services;
    }
}
