using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Safarni.Domain.Entities;

namespace Safarni.Infrastructure.Persistence.Configurations;

public sealed class TripRatingConfiguration : IEntityTypeConfiguration<TripRating>
{
    public void Configure(EntityTypeBuilder<TripRating> builder)
    {
        builder.ToTable("TripRatings");

        builder.HasKey(tr => tr.Id);

        builder.HasOne(tr => tr.SnapshotVersion)
            .WithMany(sv => sv.Ratings)
            .HasForeignKey(tr => tr.SnapshotVersionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(tr => tr.User)
            .WithMany()
            .HasForeignKey(tr => tr.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // A user can submit exactly one TripRating per snapshot
        builder.HasIndex(tr => new { tr.UserId, tr.SnapshotVersionId }).IsUnique();
    }
}