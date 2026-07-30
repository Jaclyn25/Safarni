using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Safarni.Domain.Entities;

namespace Safarni.Infrastructure.Persistence.Configurations;

public sealed class ItineraryDayConfiguration : IEntityTypeConfiguration<ItineraryDay>
{
    public void Configure(EntityTypeBuilder<ItineraryDay> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Date)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(d => d.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(d => d.Notes)
            .HasMaxLength(1000);

        builder.HasIndex(d => new { d.ItineraryId, d.DayNumber })
            .IsUnique();

        builder.HasOne(d => d.Itinerary)
            .WithMany(i => i.Days)
            .HasForeignKey(d => d.ItineraryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
