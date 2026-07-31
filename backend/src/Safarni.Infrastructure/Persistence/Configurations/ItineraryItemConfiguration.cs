using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Safarni.Domain.Entities;

namespace Safarni.Infrastructure.Persistence.Configurations;

public sealed class ItineraryItemConfiguration : IEntityTypeConfiguration<ItineraryItem>
{
    public void Configure(EntityTypeBuilder<ItineraryItem> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(i => i.Description)
            .HasMaxLength(1500);

        builder.Property(i => i.Location)
            .HasMaxLength(300);

        builder.Property(i => i.StartTime)
            .HasColumnType("time");

        builder.Property(i => i.EndTime)
            .HasColumnType("time");
	builder.Property(item => item.GooglePlaceId)
    .HasMaxLength(200);

builder.Property(item => item.Address)
    .HasMaxLength(500);

builder.Property(item => item.GoogleMapsUrl)
    .HasMaxLength(1000);

builder.Property(item => item.PhotoUrl)
    .HasMaxLength(1000);

builder.Property(item => item.Rating);

        builder.Property(item => item.PriceLevel)
            .HasMaxLength(50);

        builder.Property(item => item.RatePerNight)
            .HasColumnType("decimal(12,2)");

        builder.Property(item => item.PriceSource)
            .HasMaxLength(200);

        builder.Property(item => item.ExternalUrl)
            .HasMaxLength(1500);

        builder.Property(item => item.CostSource)
            .HasMaxLength(50);

        builder.Property(item => item.RouteTravelMode)
            .HasMaxLength(50);

        builder.Property(item => item.RouteTransitDetails)
            .HasMaxLength(500);

        builder.Property(i => i.Category)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(i => i.EstimatedCost)
            .HasColumnType("decimal(12,2)");

        builder.HasIndex(i => new { i.ItineraryDayId, i.OrderIndex });

        builder.HasOne(i => i.ItineraryDay)
            .WithMany(d => d.Items)
            .HasForeignKey(i => i.ItineraryDayId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
