using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Abstractions.Security;
using Safarni.Application.Abstractions.Storage;
using Safarni.Application.Features.Auth.GoogleLogin;
using Safarni.Infrastructure.Authentication;
using Safarni.Infrastructure.Email;
using Safarni.Infrastructure.Links;
using Safarni.Infrastructure.Persistence;
using Safarni.Infrastructure.Repositories;
using Safarni.Infrastructure.Security;
using Safarni.Infrastructure.Storage;
using Safarni.Application.Abstractions.Travel;
using Safarni.Infrastructure.Travel;

namespace Safarni.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is missing.");

        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));

        var googleClientId = configuration["GoogleSettings:ClientId"];
        if (string.IsNullOrWhiteSpace(googleClientId))
        {
            throw new InvalidOperationException(
                "GoogleSettings:ClientId is missing.");
        }

        services.AddSingleton(new GoogleAuthOptions(googleClientId));
        services.AddDbContext<ApplicationDbContext>(
            options => options.UseSqlServer(connectionString));
        services.AddMemoryCache();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<ITravelProfileRepository, TravelProfileRepository>();
        services.AddScoped<IVisaApplicationRepository, VisaApplicationRepository>();
        services.AddScoped<ITripRepository, TripRepository>();
        services.AddScoped<IItineraryRepository, ItineraryRepository>();

        services.AddScoped<IPasswordHasher, PasswordHasherService>();
        services.AddScoped<ITokenService, JwtTokenService>();

        services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));
        services.Configure<FrontendSettings>(configuration.GetSection("Frontend"));
        services.AddScoped<IEmailService, EmailService>();
        services.AddSingleton<IPasswordResetLinkFactory, PasswordResetLinkFactory>();
        services.AddSingleton<IEmailVerificationLinkFactory, EmailVerificationLinkFactory>();
        services.AddScoped<IProfilePhotoStorage, LocalProfilePhotoStorage>();
        services.AddScoped<IUserSettingsRepository, UserSettingsRepository>();

        services.AddHttpClient<
            IItineraryTravelDataService,
            SerpApiTravelDataService>();

        services.AddHttpClient<
            IItineraryRouteService,
            GoogleRoutesService>();

        services.AddHttpClient<
            ITravelLocationCatalog,
            CountriesNowLocationCatalog>(
            client =>
            {
                client.BaseAddress = new Uri(
                    "https://countriesnow.space/api/v0.1/");
                client.Timeout = TimeSpan.FromSeconds(30);
            });

        services.AddHttpClient<
            IFlightAirportService,
            SerpApiFlightAirportService>();

        return services;
    }
}
