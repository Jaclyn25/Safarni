using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripMindAI.Domain.Entities;

namespace TripMindAI.Infrastructure.Data.Configurations;

public class HotelSearchConfiguration : IEntityTypeConfiguration<HotelSearch>
{
    public void Configure(EntityTypeBuilder<HotelSearch> builder)
    {
        builder.HasKey(h => h.Id);

        builder.Property(h => h.DestinationCity)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(h => h.DestinationCountry)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(h => h.Provider)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(h => h.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(h => h.User)
            .WithMany(u => u.HotelSearches)
            .HasForeignKey(h => h.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(h => h.TripPlan)
            .WithMany(t => t.HotelSearches)
            .HasForeignKey(h => h.TripPlanId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(h => h.Offers)
            .WithOne(o => o.HotelSearch)
            .HasForeignKey(o => o.HotelSearchId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
