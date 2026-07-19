using Microsoft.EntityFrameworkCore;
using Safarni.Domain.Entities;

namespace Safarni.Infrastructure.Persistence;

public sealed class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<TravelPreference> TravelPreferences => Set<TravelPreference>();

    public DbSet<PassportInfo> PassportInfos => Set<PassportInfo>();

    public DbSet<VisaApplication> VisaApplications => Set<VisaApplication>();
    public DbSet<UserSettings> UserSettings => Set<UserSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
