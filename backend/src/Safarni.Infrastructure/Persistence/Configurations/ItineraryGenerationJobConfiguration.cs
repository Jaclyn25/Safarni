using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Safarni.Domain.Entities;

namespace Safarni.Infrastructure.Persistence.Configurations;

public sealed class ItineraryGenerationJobConfiguration
    : IEntityTypeConfiguration<ItineraryGenerationJob>
{
    public void Configure(EntityTypeBuilder<ItineraryGenerationJob> builder)
    {
        builder.HasKey(job => job.Id);

        builder.Property(job => job.Preferences)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(job => job.BudgetAmount)
            .HasColumnType("decimal(12,2)");

        builder.Property(job => job.CurrencyCode)
            .IsRequired()
            .HasMaxLength(3)
            .IsUnicode(false);

        builder.Property(job => job.IdempotencyKey)
            .HasMaxLength(100);

        builder.Property(job => job.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(job => job.Stage)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(job => job.ErrorCode)
            .HasMaxLength(100);

        builder.Property(job => job.ErrorMessage)
            .HasMaxLength(1000);

        builder.Property(job => job.WorkerId)
            .HasMaxLength(100);

        builder.Property(job => job.RowVersion)
            .IsRowVersion();

        builder.HasIndex(job => new { job.TripId, job.CreatedAt });

        // Prevent duplicate queued/running generation for the same trip.
        builder.HasIndex(job => job.TripId)
            .IsUnique()
            .HasFilter("[IsActive] = 1");

        builder.HasIndex(job => new { job.UserId, job.IdempotencyKey })
            .IsUnique()
            .HasFilter("[IdempotencyKey] IS NOT NULL");

        builder.HasOne(job => job.Trip)
            .WithMany(trip => trip.GenerationJobs)
            .HasForeignKey(job => job.TripId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
