using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripMindAI.Domain.Entities;

namespace TripMindAI.Infrastructure.Data.Configurations;

public class PointOfInterestConfiguration : IEntityTypeConfiguration<PointOfInterest>
{
    public void Configure(EntityTypeBuilder<PointOfInterest> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.Category)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(p => p.Address)
            .HasMaxLength(500);

        builder.Property(p => p.Latitude)
            .HasPrecision(9, 6);

        builder.Property(p => p.Longitude)
            .HasPrecision(9, 6);

        builder.Property(p => p.Rating)
            .HasPrecision(3, 2);

        builder.Property(p => p.ExternalPlaceId)
            .HasMaxLength(200);

        builder.Property(p => p.Provider)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(p => p.EstimatedCostUsd)
            .HasPrecision(18, 2);

        builder.Property(p => p.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(p => p.Destination)
            .WithMany(d => d.PointsOfInterest)
            .HasForeignKey(p => p.DestinationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
