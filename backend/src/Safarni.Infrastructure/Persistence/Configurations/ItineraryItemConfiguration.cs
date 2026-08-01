using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Safarni.Domain.Entities;

namespace Safarni.Infrastructure.Persistence.Configurations;

public sealed class ItineraryItemConfiguration
    : IEntityTypeConfiguration<ItineraryItem>
{
    public void Configure(EntityTypeBuilder<ItineraryItem> builder)
    {
        builder.HasKey(item => item.Id);

        builder.Property(item => item.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(item => item.Description)
            .HasMaxLength(1500);

        builder.Property(item => item.Location)
            .HasMaxLength(300);

        builder.Property(item => item.StartTime)
            .HasColumnType("time");

        builder.Property(item => item.EndTime)
            .HasColumnType("time");

        builder.Property(item => item.GooglePlaceId)
            .HasMaxLength(200);

        builder.Property(item => item.Address)
            .HasMaxLength(500);

        builder.Property(item => item.GoogleMapsUrl)
            .HasMaxLength(1000);

        builder.Property(item => item.PhotoUrl)
            .HasMaxLength(1000);

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

        builder.Property(item => item.FlightDepartureAirportCode)
            .HasMaxLength(3)
            .IsUnicode(false);

        builder.Property(item => item.FlightDepartureAirportName)
            .HasMaxLength(200);

        builder.Property(item => item.FlightArrivalAirportCode)
            .HasMaxLength(3)
            .IsUnicode(false);

        builder.Property(item => item.FlightArrivalAirportName)
            .HasMaxLength(200);

        builder.Property(item => item.FlightAirlines)
            .HasMaxLength(300);

        builder.Property(item => item.FlightNumbers)
            .HasMaxLength(200);

        builder.Property(item => item.RouteTravelMode)
            .HasMaxLength(50);

        builder.Property(item => item.RouteTransitDetails)
            .HasMaxLength(500);

        builder.Property(item => item.Category)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(item => item.EstimatedCost)
            .HasColumnType("decimal(12,2)");

        builder.Property(item => item.RowVersion)
            .IsRowVersion();

        builder.HasIndex(item => new
        {
            item.ItineraryDayId,
            item.OrderIndex
        });

        builder.HasOne(item => item.ItineraryDay)
            .WithMany(day => day.Items)
            .HasForeignKey(item => item.ItineraryDayId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
