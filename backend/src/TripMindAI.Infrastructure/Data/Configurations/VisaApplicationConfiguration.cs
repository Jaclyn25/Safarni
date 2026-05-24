using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripMindAI.Domain.Entities;

namespace TripMindAI.Infrastructure.Data.Configurations;

public class VisaApplicationConfiguration : IEntityTypeConfiguration<VisaApplication>
{
    public void Configure(EntityTypeBuilder<VisaApplication> builder)
    {
        builder.HasKey(v => v.Id);

        builder.Property(v => v.NationalityCountry)
            .HasMaxLength(100)
            .HasDefaultValue("Egypt");

        builder.Property(v => v.DestinationCountry)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(v => v.VisaType)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(v => v.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(v => v.RiskLevel)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(v => v.Summary)
            .HasMaxLength(2000);

        builder.Property(v => v.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(v => v.User)
            .WithMany(u => u.VisaApplications)
            .HasForeignKey(v => v.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(v => v.UploadedDocuments)
            .WithOne(d => d.VisaApplication)
            .HasForeignKey(d => d.VisaApplicationId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(v => v.Issues)
            .WithOne(i => i.VisaApplication)
            .HasForeignKey(i => i.VisaApplicationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
