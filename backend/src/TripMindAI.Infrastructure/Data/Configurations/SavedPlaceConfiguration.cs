using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripMindAI.Domain.Entities;

namespace TripMindAI.Infrastructure.Data.Configurations;

public class SavedPlaceConfiguration : IEntityTypeConfiguration<SavedPlace>
{
    public void Configure(EntityTypeBuilder<SavedPlace> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.SavedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(s => s.User)
            .WithMany(u => u.SavedPlaces)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.PointOfInterest)
            .WithMany(p => p.SavedByUsers)
            .HasForeignKey(s => s.PointOfInterestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
