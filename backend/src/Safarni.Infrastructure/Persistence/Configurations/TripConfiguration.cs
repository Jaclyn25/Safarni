using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Safarni.Domain.Entities;

namespace Safarni.Infrastructure.Persistence.Configurations;

public sealed class TripConfiguration : IEntityTypeConfiguration<Trip>
{
    public void Configure(EntityTypeBuilder<Trip> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.Destination)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.OriginCountry)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(t => t.OriginCountryCode)
            .HasMaxLength(2)
            .IsUnicode(false);

        builder.Property(t => t.OriginCity)
            .HasMaxLength(100);

        builder.Property(t => t.OriginAirportCode)
            .HasMaxLength(3)
            .IsUnicode(false);

        builder.Property(t => t.OriginAirportName)
            .HasMaxLength(200);

        builder.Property(t => t.SelectedCitiesJson)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(t => t.StartDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(t => t.EndDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(t => t.CreatedAt)
            .IsRequired();

        builder.Property(t => t.RowVersion)
            .IsRowVersion();

        builder.HasIndex(t => new { t.UserId, t.CreatedAt });

        builder.HasOne(t => t.User)
            .WithMany(u => u.Trips)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
