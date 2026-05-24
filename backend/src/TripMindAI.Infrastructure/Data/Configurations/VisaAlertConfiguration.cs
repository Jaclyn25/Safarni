using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripMindAI.Domain.Entities;

namespace TripMindAI.Infrastructure.Data.Configurations;

public class VisaAlertConfiguration : IEntityTypeConfiguration<VisaAlert>
{
    public void Configure(EntityTypeBuilder<VisaAlert> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.CountryFrom)
            .HasMaxLength(100)
            .HasDefaultValue("Egypt");

        builder.Property(a => a.CountryTo)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.AlertType)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(a => a.Severity)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(a => a.Title)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(a => a.Message)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(a => a.SourceUrl)
            .HasMaxLength(500);

        builder.Property(a => a.PublishedAt)
            .HasDefaultValueSql("GETUTCDATE()");
    }
}
