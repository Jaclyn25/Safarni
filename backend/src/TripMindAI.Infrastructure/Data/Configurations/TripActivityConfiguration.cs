using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripMindAI.Domain.Entities;

namespace TripMindAI.Infrastructure.Data.Configurations;

public class TripActivityConfiguration : IEntityTypeConfiguration<TripActivity>
{
    public void Configure(EntityTypeBuilder<TripActivity> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.TimeSlot)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(a => a.Title)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(a => a.Description)
            .HasMaxLength(2000);

        builder.Property(a => a.LocationName)
            .HasMaxLength(200);

        builder.Property(a => a.Latitude)
            .HasPrecision(9, 6);

        builder.Property(a => a.Longitude)
            .HasPrecision(9, 6);

        builder.Property(a => a.EstimatedCostUsd)
            .HasPrecision(18, 2);

        builder.Property(a => a.EstimatedCostEgp)
            .HasPrecision(18, 2);

        builder.Property(a => a.ActivityType)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(a => a.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(a => a.TripDay)
            .WithMany(d => d.Activities)
            .HasForeignKey(a => a.TripDayId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.PointOfInterest)
            .WithMany(p => p.TripActivities)
            .HasForeignKey(a => a.PointOfInterestId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
