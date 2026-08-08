using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Safarni.Application.Abstractions.Security;
using Safarni.Application.Abstractions.Storage;
using Safarni.Domain.Entities;
using Safarni.Domain.Enums;

namespace Safarni.Infrastructure.Persistence;

/// <summary>
/// Optional, idempotent development seed for the marketplace review workflow.
/// Enable with MarketplaceSeed:Enabled and provide MarketplaceSeed:Password.
/// It never runs in production unless explicitly enabled.
/// </summary>
public sealed class MarketplaceTestDataSeeder
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAgencyVerificationDocumentStorage _documentStorage;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MarketplaceTestDataSeeder> _logger;

    public MarketplaceTestDataSeeder(
        ApplicationDbContext dbContext,
        IPasswordHasher passwordHasher,
        IAgencyVerificationDocumentStorage documentStorage,
        IConfiguration configuration,
        ILogger<MarketplaceTestDataSeeder> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _documentStorage = documentStorage;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!bool.TryParse(_configuration["MarketplaceSeed:Enabled"], out var enabled) || !enabled)
        {
            return;
        }

        var password = _configuration["MarketplaceSeed:Password"];
        if (string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning(
                "Marketplace test seed is enabled but MarketplaceSeed:Password is missing.");
            return;
        }

        await EnsureAdminAsync(password, cancellationToken);
        await EnsureAgencyAsync(
            "verified.agency@safarni.local",
            "Safarni Verified Tours",
            AgencyVerificationStatus.Verified,
            password,
            includeDocuments: true,
            cancellationToken);
        await EnsureAgencyAsync(
            "pending.agency@safarni.local",
            "Safarni Pending Travel",
            AgencyVerificationStatus.Pending,
            password,
            includeDocuments: false,
            cancellationToken);

        _logger.LogInformation("Marketplace development seed is ready.");
    }

    private async Task EnsureAdminAsync(string password, CancellationToken cancellationToken)
    {
        const string email = "marketplace.admin@safarni.local";
        if (await _dbContext.Users.AnyAsync(user => user.Email == email, cancellationToken))
        {
            return;
        }

        _dbContext.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            FullName = "Marketplace Administrator",
            Email = email,
            PasswordHash = _passwordHasher.Hash(password),
            Role = UserRole.Admin,
            IsActive = true,
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureAgencyAsync(
        string email,
        string companyName,
        AgencyVerificationStatus status,
        string password,
        bool includeDocuments,
        CancellationToken cancellationToken)
    {
        if (await _dbContext.Users.AnyAsync(user => user.Email == email, cancellationToken))
        {
            return;
        }

        var now = DateTime.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = status == AgencyVerificationStatus.Verified
                ? "Verified Agency Manager"
                : "Pending Agency Manager",
            Email = email,
            PhoneNumber = "+20 100 000 0000",
            PasswordHash = _passwordHasher.Hash(password),
            Role = UserRole.TravelAgency,
            IsActive = true,
            IsEmailVerified = true,
            CreatedAt = now
        };
        var agency = new TravelAgency
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            CompanyName = companyName,
            Description = "Development account for testing Safarni's agency verification workflow.",
            Address = "Smart Village",
            City = "Giza",
            Country = "Egypt",
            CompanyPhone = "+20 2 0000 0000",
            WebsiteUrl = "https://example.com",
            VerificationStatus = status,
            VerificationDecisionReason = status == AgencyVerificationStatus.Verified
                ? "Approved development seed account."
                : null,
            VerificationReviewedAt = status == AgencyVerificationStatus.Verified ? now : null,
            CreatedAt = now
        };
        user.TravelAgency = agency;

        var storedReferences = new List<string>();
        try
        {
            if (includeDocuments)
            {
                foreach (var type in Enum.GetValues<AgencyVerificationDocumentType>())
                {
                    var content = Encoding.UTF8.GetBytes(
                        $"%PDF-1.4\n% Safarni development verification document: {type}\n%%EOF");
                    await using var stream = new MemoryStream(content, writable: false);
                    var storageReference = await _documentStorage.SaveAsync(
                        agency.Id,
                        stream,
                        ".pdf",
                        cancellationToken);
                    storedReferences.Add(storageReference);
                    agency.VerificationDocuments.Add(new AgencyVerificationDocument
                    {
                        Id = Guid.NewGuid(),
                        TravelAgencyId = agency.Id,
                        TravelAgency = agency,
                        DocumentType = type,
                        DocumentNumber = $"SEED-{type.ToString().ToUpperInvariant()}",
                        StorageReference = storageReference,
                        OriginalFileName = $"{type}.pdf",
                        ContentType = "application/pdf",
                        FileSizeBytes = content.LongLength,
                        ExpiryDate = now.Date.AddYears(1),
                        UploadedAt = now,
                        ReviewStatus = status == AgencyVerificationStatus.Verified
                            ? AgencyDocumentReviewStatus.Approved
                            : AgencyDocumentReviewStatus.Pending,
                        ReviewedAt = status == AgencyVerificationStatus.Verified ? now : null
                    });
                }
            }

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            foreach (var storageReference in storedReferences)
            {
                await _documentStorage.DeleteAsync(storageReference, CancellationToken.None);
            }

            throw;
        }
    }
}
