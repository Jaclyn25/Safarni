using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripMindAI.Domain.Entities;

namespace TripMindAI.Infrastructure.Data.Configurations;

public class TripPlanConfiguration : IEntityTypeConfiguration<TripPlan>
{
    public void Configure(EntityTypeBuilder<TripPlan> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(t => t.DestinationCountry)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(t => t.DestinationCity)
            .HasMaxLength(100);

        builder.Property(t => t.BudgetUsd)
            .HasPrecision(18, 2);

        builder.Property(t => t.BudgetEgp)
            .HasPrecision(18, 2);

        builder.Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(t => t.AiSummary)
            .HasMaxLength(4000);

        builder.Property(t => t.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(t => t.User)
            .WithMany(u => u.TripPlans)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Conversation)
            .WithMany(c => c.TripPlans)
            .HasForeignKey(t => t.ConversationId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(t => t.VisaApplication)
            .WithMany(v => v.TripPlans)
            .HasForeignKey(t => t.VisaApplicationId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(t => t.Days)
            .WithOne(d => d.TripPlan)
            .HasForeignKey(d => d.TripPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.CostItems)
            .WithOne(c => c.TripPlan)
            .HasForeignKey(c => c.TripPlanId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
