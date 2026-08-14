using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Safarni.Domain.Entities;

namespace Safarni.Infrastructure.Persistence.Configurations;

public class SnapshotVersionConfiguration
    : IEntityTypeConfiguration<SnapshotVersion>
{
    public void Configure(EntityTypeBuilder<SnapshotVersion> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ItineraryJson)
            .IsRequired();

        builder.Property(x => x.CostAmount)
            .HasPrecision(18, 2);

        builder.Property(x => x.IncludedPhotoIdsJson)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasOne(x => x.Snapshot)
            .WithMany(x => x.Versions)
            .HasForeignKey(x => x.SnapshotId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new
        {
            x.SnapshotId,
            x.VersionNumber
        })
        .IsUnique();
    }
}