using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripMindAI.Domain.Entities;

namespace TripMindAI.Infrastructure.Data.Configurations;

public class FlightOfferConfiguration : IEntityTypeConfiguration<FlightOffer>
{
    public void Configure(EntityTypeBuilder<FlightOffer> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.AirlineCode)
            .HasMaxLength(10);

        builder.Property(o => o.AirlineName)
            .HasMaxLength(100);

        builder.Property(o => o.FlightNumber)
            .HasMaxLength(20);

        builder.Property(o => o.OriginAirportCode)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(o => o.DestinationAirportCode)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(o => o.PriceUsd)
            .HasPrecision(18, 2);

        builder.Property(o => o.PriceEgp)
            .HasPrecision(18, 2);

        builder.Property(o => o.RawResponseJson)
            .HasMaxLength(8000)
            .HasDefaultValue("{}");

        builder.HasOne(o => o.FlightSearch)
            .WithMany(f => f.Offers)
            .HasForeignKey(o => o.FlightSearchId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
