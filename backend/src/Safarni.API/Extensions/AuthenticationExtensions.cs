using System.Text;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Safarni.Domain.Enums;
using Safarni.Infrastructure.Authentication;
using Safarni.Application.Abstractions.Security;

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
            options.AddPolicy(AccountPolicies.TravelerOnly, policy =>
                policy.RequireRole(UserRole.User.ToString()));
            options.AddPolicy(AccountPolicies.TravelAgencyOnly, policy =>
                policy.RequireRole(UserRole.TravelAgency.ToString()));
            options.AddPolicy(AccountPolicies.VerifiedTravelAgency, policy =>
            {
                policy.RequireRole(UserRole.TravelAgency.ToString());
                policy.AddRequirements(new VerifiedTravelAgencyRequirement());
            });
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

        services.AddScoped<IAuthorizationHandler, VerifiedTravelAgencyRequirementHandler>();

        return services;
    }
}

public static class AccountPolicies
{
    public const string TravelerOnly = "TravelerOnly";
    public const string TravelAgencyOnly = "TravelAgencyOnly";
    public const string VerifiedTravelAgency = "VerifiedTravelAgency";
}

public static class AdminPolicies
{
    public const string AdminOnly = "AdminOnly";
    public const string AdminOrSupport = "AdminOrSupport";
    public const string AdminOrContentManager = "AdminOrContentManager";
    public const string AdminOrAnalyst = "AdminOrAnalyst";
}

public sealed class VerifiedTravelAgencyRequirement : IAuthorizationRequirement;

public sealed class VerifiedTravelAgencyRequirementHandler
    : AuthorizationHandler<VerifiedTravelAgencyRequirement>
{
    private readonly ITravelAgencyAuthorizationService _authorizationService;

    public VerifiedTravelAgencyRequirementHandler(
        ITravelAgencyAuthorizationService authorizationService)
    {
        _authorizationService = authorizationService;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        VerifiedTravelAgencyRequirement requirement)
    {
        var userIdValue = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userIdValue, out var userId) &&
            await _authorizationService.IsVerifiedAsync(userId))
        {
            context.Succeed(requirement);
        }
    }
}
