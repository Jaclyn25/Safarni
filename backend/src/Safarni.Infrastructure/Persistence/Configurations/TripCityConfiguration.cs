using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Safarni.Domain.Entities;

namespace Safarni.Infrastructure.Persistence.Configurations;

public sealed class TripCityConfiguration : IEntityTypeConfiguration<TripCity>
{
    public void Configure(EntityTypeBuilder<TripCity> builder)
    {
        builder.HasKey(city => city.Id);

        builder.Property(city => city.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(city => city.Days)
            .IsRequired();

        builder.Property(city => city.OrderIndex)
            .IsRequired();

        builder.Property(city => city.AirportCode)
            .HasMaxLength(3)
            .IsUnicode(false);

        builder.Property(city => city.AirportName)
            .HasMaxLength(200);

        builder.HasIndex(city => new { city.TripId, city.OrderIndex })
            .IsUnique();

        builder.HasOne(city => city.Trip)
            .WithMany(trip => trip.Cities)
            .HasForeignKey(city => city.TripId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
