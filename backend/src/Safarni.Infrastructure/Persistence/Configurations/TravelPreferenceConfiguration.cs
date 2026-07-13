using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Safarni.Domain.Entities;

namespace Safarni.Infrastructure.Persistence.Configurations;

public sealed class TravelPreferenceConfiguration : IEntityTypeConfiguration<TravelPreference>
{
    public void Configure(EntityTypeBuilder<TravelPreference> builder)
    {
        builder.ToTable("TravelPreferences");

        builder.HasKey(preference => preference.Id);

        builder.Property(preference => preference.PreferredDestinationCountry)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(preference => preference.TravelPurpose)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(preference => preference.TravelStyle)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(preference => preference.MinBudget)
            .HasPrecision(18, 2);

        builder.Property(preference => preference.MaxBudget)
            .HasPrecision(18, 2);

        builder.Property(preference => preference.UpdatedAt)
            .IsRequired();

        builder.HasOne(preference => preference.User)
            .WithOne(user => user.TravelPreference)
            .HasForeignKey<TravelPreference>(preference => preference.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(preference => preference.UserId)
            .IsUnique();
    }
}
