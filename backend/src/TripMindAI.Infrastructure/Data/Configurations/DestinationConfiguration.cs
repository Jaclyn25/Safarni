using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripMindAI.Domain.Entities;

namespace TripMindAI.Infrastructure.Data.Configurations;

public class DestinationConfiguration : IEntityTypeConfiguration<Destination>
{
    public void Configure(EntityTypeBuilder<Destination> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Country)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(d => d.City)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(d => d.Description)
            .HasMaxLength(2000);

        builder.Property(d => d.AverageDailyBudgetUsd)
            .HasPrecision(18, 2);

        builder.Property(d => d.BestSeason)
            .HasMaxLength(100);

        builder.Property(d => d.Latitude)
            .HasPrecision(9, 6);

        builder.Property(d => d.Longitude)
            .HasPrecision(9, 6);

        builder.HasMany(d => d.PointsOfInterest)
            .WithOne(p => p.Destination)
            .HasForeignKey(p => p.DestinationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
