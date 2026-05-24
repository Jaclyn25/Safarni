using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripMindAI.Domain.Entities;

namespace TripMindAI.Infrastructure.Data.Configurations;

public class FlightSearchConfiguration : IEntityTypeConfiguration<FlightSearch>
{
    public void Configure(EntityTypeBuilder<FlightSearch> builder)
    {
        builder.HasKey(f => f.Id);

        builder.Property(f => f.OriginAirportCode)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(f => f.DestinationAirportCode)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(f => f.CurrencyCode)
            .HasMaxLength(3)
            .HasDefaultValue("USD");

        builder.Property(f => f.Provider)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(f => f.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(f => f.User)
            .WithMany(u => u.FlightSearches)
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(f => f.TripPlan)
            .WithMany(t => t.FlightSearches)
            .HasForeignKey(f => f.TripPlanId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(f => f.Offers)
            .WithOne(o => o.FlightSearch)
            .HasForeignKey(o => o.FlightSearchId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
