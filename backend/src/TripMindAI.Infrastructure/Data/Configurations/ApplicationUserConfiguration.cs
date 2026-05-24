using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripMindAI.Domain.Entities;

namespace TripMindAI.Infrastructure.Data.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("AspNetUsers");

        builder.Property(u => u.FullName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(u => u.NationalityCountry)
            .HasMaxLength(100)
            .HasDefaultValue("Egypt");

        builder.Property(u => u.PreferredLanguage)
            .HasMaxLength(50)
            .HasDefaultValue("ar");

        builder.Property(u => u.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");
    }
}
