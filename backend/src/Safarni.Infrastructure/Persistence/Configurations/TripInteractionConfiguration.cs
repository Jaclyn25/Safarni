using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Safarni.Domain.Entities;

namespace Safarni.Infrastructure.Persistence.Configurations;

public sealed class TripInteractionConfiguration : IEntityTypeConfiguration<TripInteraction>
{
    public void Configure(EntityTypeBuilder<TripInteraction> builder)
    {
        builder.ToTable("TripInteractions");

        builder.HasKey(ti => ti.Id);

        builder.HasOne(ti => ti.SnapshotVersion)
            .WithMany(sv => sv.Interactions)
            .HasForeignKey(ti => ti.SnapshotVersionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ti => ti.User)
            .WithMany()
            .HasForeignKey(ti => ti.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // A user can only have one Like and one Save per SnapshotVersion. Reports might also be limited to 1.
        builder.HasIndex(ti => new { ti.UserId, ti.SnapshotVersionId, ti.Type }).IsUnique();
    }
}