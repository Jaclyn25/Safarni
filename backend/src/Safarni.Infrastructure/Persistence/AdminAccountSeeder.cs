using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Abstractions.Security;
using Safarni.Domain.Entities;
using Safarni.Domain.Enums;

namespace Safarni.Infrastructure.Persistence;

/// <summary>
/// Creates the first administrator account from configuration
/// (Admin:Email, Admin:Password, Admin:FullName - e.g. user-secrets or .env).
/// Runs only when credentials are configured and never duplicates accounts.
/// </summary>
public sealed class AdminAccountSeeder
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AdminAccountSeeder> _logger;

    public AdminAccountSeeder(
        IUserRepository users,
        IPasswordHasher passwordHasher,
        IConfiguration configuration,
        ILogger<AdminAccountSeeder> logger)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var email = _configuration["Admin:Email"]?.Trim();
        var password = _configuration["Admin:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogInformation(
                "Admin seed skipped: set Admin:Email and Admin:Password to create the first admin account.");
            return;
        }

        var normalizedEmail = email.ToLowerInvariant();
        var existing = await _users.GetByEmailAsync(normalizedEmail, cancellationToken);

        if (existing is null)
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                FullName = _configuration["Admin:FullName"]?.Trim() ?? "Safarni Administrator",
                Email = normalizedEmail,
                PasswordHash = _passwordHasher.Hash(password),
                Role = UserRole.Admin,
                IsActive = true,
                IsEmailVerified = true,
                CreatedAt = DateTime.UtcNow
            };

            await _users.AddAsync(user, cancellationToken);
            _logger.LogInformation("Seeded admin account for {Email}.", normalizedEmail);
            return;
        }

        if (existing.Role != UserRole.Admin)
        {
            existing.Role = UserRole.Admin;
            existing.IsEmailVerified = true;
            existing.UpdatedAt = DateTime.UtcNow;
            await _users.UpdateAsync(existing, cancellationToken);
            _logger.LogInformation(
                "Promoted existing account {Email} to Admin role.", normalizedEmail);
        }
        else
        {
            _logger.LogInformation(
                "Admin account {Email} already exists. Seed is a no-op.", normalizedEmail);
        }
    }
}
