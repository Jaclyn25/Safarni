using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Safarni.Domain.Enums;
using Safarni.Infrastructure.Authentication;

namespace Safarni.API.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>()
            ?? throw new InvalidOperationException("JwtSettings configuration is missing.");

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                ClockSkew = TimeSpan.Zero
            };
        });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(AdminPolicies.AdminOnly, policy =>
                policy.RequireRole(UserRole.Admin.ToString()));
            options.AddPolicy(AdminPolicies.AdminOrSupport, policy =>
                policy.RequireRole(
                    UserRole.Admin.ToString(),
                    UserRole.Support.ToString()));
            options.AddPolicy(AdminPolicies.AdminOrContentManager, policy =>
                policy.RequireRole(
                    UserRole.Admin.ToString(),
                    UserRole.ContentManager.ToString()));
            options.AddPolicy(AdminPolicies.AdminOrAnalyst, policy =>
                policy.RequireRole(
                    UserRole.Admin.ToString(),
                    UserRole.Analyst.ToString()));
        });

        return services;
    }
}

public static class AdminPolicies
{
    public const string AdminOnly = "AdminOnly";
    public const string AdminOrSupport = "AdminOrSupport";
    public const string AdminOrContentManager = "AdminOrContentManager";
    public const string AdminOrAnalyst = "AdminOrAnalyst";
}