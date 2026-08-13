using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Safarni.Domain.Entities;

namespace Safarni.Infrastructure.Persistence.Configurations;

public class PublicTripSnapshotConfiguration
    : IEntityTypeConfiguration<PublicTripSnapshot>
{
    public void Configure(EntityTypeBuilder<PublicTripSnapshot> builder)
    {
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Trip)
            .WithOne(x => x.PublicSnapshot)
            .HasForeignKey<PublicTripSnapshot>(x => x.TripId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Versions)
            .WithOne(x => x.Snapshot)
            .HasForeignKey(x => x.SnapshotId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.TripId)
            .IsUnique();
    }
}