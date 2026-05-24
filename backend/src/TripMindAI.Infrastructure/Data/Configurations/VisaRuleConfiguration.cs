using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripMindAI.Domain.Entities;

namespace TripMindAI.Infrastructure.Data.Configurations;

public class VisaRuleConfiguration : IEntityTypeConfiguration<VisaRule>
{
    public void Configure(EntityTypeBuilder<VisaRule> builder)
    {
        builder.HasKey(v => v.Id);

        builder.Property(v => v.CountryFrom)
            .HasMaxLength(100)
            .HasDefaultValue("Egypt");

        builder.Property(v => v.CountryTo)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(v => v.VisaType)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(v => v.Notes)
            .HasMaxLength(2000);

        builder.Property(v => v.OfficialSourceUrl)
            .HasMaxLength(500);

        builder.Property(v => v.LastUpdated)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasMany(v => v.Documents)
            .WithOne(d => d.VisaRule)
            .HasForeignKey(d => d.VisaRuleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
