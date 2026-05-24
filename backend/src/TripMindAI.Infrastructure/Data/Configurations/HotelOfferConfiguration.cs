using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripMindAI.Domain.Entities;

namespace TripMindAI.Infrastructure.Data.Configurations;

public class HotelOfferConfiguration : IEntityTypeConfiguration<HotelOffer>
{
    public void Configure(EntityTypeBuilder<HotelOffer> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.HotelName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(o => o.Address)
            .HasMaxLength(500);

        builder.Property(o => o.Latitude)
            .HasPrecision(9, 6);

        builder.Property(o => o.Longitude)
            .HasPrecision(9, 6);

        builder.Property(o => o.Rating)
            .HasPrecision(3, 2);

        builder.Property(o => o.PriceUsd)
            .HasPrecision(18, 2);

        builder.Property(o => o.PriceEgp)
            .HasPrecision(18, 2);

        builder.Property(o => o.ImageUrl)
            .HasMaxLength(500);

        builder.Property(o => o.RawResponseJson)
            .HasMaxLength(8000)
            .HasDefaultValue("{}");

        builder.HasOne(o => o.HotelSearch)
            .WithMany(h => h.Offers)
            .HasForeignKey(o => o.HotelSearchId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
