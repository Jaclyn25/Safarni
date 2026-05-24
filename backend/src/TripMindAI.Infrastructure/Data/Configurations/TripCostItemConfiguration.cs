using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripMindAI.Domain.Entities;

namespace TripMindAI.Infrastructure.Data.Configurations;

public class TripCostItemConfiguration : IEntityTypeConfiguration<TripCostItem>
{
    public void Configure(EntityTypeBuilder<TripCostItem> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Category)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(c => c.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.AmountUsd)
            .HasPrecision(18, 2);

        builder.Property(c => c.AmountEgp)
            .HasPrecision(18, 2);

        builder.Property(c => c.Notes)
            .HasMaxLength(500);

        builder.Property(c => c.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(c => c.TripPlan)
            .WithMany(t => t.CostItems)
            .HasForeignKey(c => c.TripPlanId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
