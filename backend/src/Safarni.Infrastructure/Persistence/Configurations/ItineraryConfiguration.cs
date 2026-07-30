using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Safarni.Domain.Entities;

namespace Safarni.Infrastructure.Persistence.Configurations;

public sealed class ItineraryConfiguration : IEntityTypeConfiguration<Itinerary>
{
    public void Configure(EntityTypeBuilder<Itinerary> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Preferences)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(i => i.CurrencyCode)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(i => i.GeneratedBy)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(i => i.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(i => i.CreatedAt)
            .IsRequired();

        builder.Property(i => i.UpdatedAt)
            .IsRequired();

	    builder.Property(itinerary => itinerary.BudgetAmount)
            .HasColumnType("decimal(12,2)")
            .IsRequired();

        builder.Property(itinerary => itinerary.TravelerCount)
            .IsRequired();

        builder.HasIndex(i => new { i.TripId, i.IsActive });

        builder.HasOne(i => i.Trip)
            .WithMany(t => t.Itineraries)
            .HasForeignKey(i => i.TripId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
