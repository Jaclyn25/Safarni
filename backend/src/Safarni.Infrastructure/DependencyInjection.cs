using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Safarni.Application.Abstractions.AI;
using Safarni.Application.Abstractions.Auditing;
using Safarni.Application.Abstractions.BackgroundJobs;
using Safarni.Application.Abstractions.Integrations;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Abstractions.Security;
using Safarni.Application.Abstractions.Storage;
using Safarni.Application.Features.Auth.GoogleLogin;
using Safarni.Infrastructure.AI;
using Safarni.Infrastructure.Auditing;
using Safarni.Infrastructure.Authentication;
using Safarni.Infrastructure.Integrations;
using Safarni.Infrastructure.Email;
using Safarni.Infrastructure.Links;
using Safarni.Infrastructure.Persistence;
using Safarni.Infrastructure.Repositories;
using Safarni.Infrastructure.Security;
using Safarni.Infrastructure.Storage;
using Safarni.Application.Abstractions.Travel;
using Safarni.Infrastructure.Travel;
using Safarni.Infrastructure.BackgroundJobs;

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
        services.AddScoped<
            IItineraryGenerationJobRepository,
            ItineraryGenerationJobRepository>();
        services.AddScoped<IAdminRepository, AdminRepository>();

        services.AddScoped<IPasswordHasher, PasswordHasherService>();
        services.AddScoped<ITokenService, JwtTokenService>();

        services.AddScoped<IAuditLogWriter, AuditLogWriter>();
        services.AddScoped<IAIRequestLogWriter, AIRequestLogWriter>();
        services.AddScoped<
            IExternalServiceHealthChecker,
            ExternalServiceHealthChecker>();

        services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));
        services.Configure<FrontendSettings>(configuration.GetSection("Frontend"));
        services.AddScoped<IEmailService, EmailService>();
        services.AddSingleton<IPasswordResetLinkFactory, PasswordResetLinkFactory>();
        services.AddSingleton<IEmailVerificationLinkFactory, EmailVerificationLinkFactory>();
        services.AddScoped<IProfilePhotoStorage, LocalProfilePhotoStorage>();
        services.AddScoped<IUserSettingsRepository, UserSettingsRepository>();

        services.AddHttpClient<
            IItineraryTravelDataService,
            SerpApiTravelDataService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(25);
            });

        services.AddHttpClient<
            IItineraryRouteService,
            GoogleRoutesService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(15);
            });

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
            SerpApiFlightAirportService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(25);
            });

        services.AddHttpClient<
            IGeoapifyService,
            GeoapifyService>(
            (serviceProvider, client) =>
            {
                var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                var baseUrl = configuration["GeoapifySettings:BaseUrl"] ?? "https://api.geoapify.com/";
                client.BaseAddress = new Uri(baseUrl);
                client.Timeout = TimeSpan.FromSeconds(15);
            });

        // One durable worker per API process. Database row locks and filtered
        // unique indexes keep processing safe when several API instances run.
        services.AddSingleton<
            IItineraryGenerationQueueSignal,
            ItineraryGenerationQueueSignal>();
        services.AddHostedService<ItineraryGenerationWorker>();

        return services;
    }
}
