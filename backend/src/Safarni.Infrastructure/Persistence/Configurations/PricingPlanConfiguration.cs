using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Safarni.Domain.Entities;

namespace Safarni.Infrastructure.Persistence.Configurations;

public sealed class PricingPlanConfiguration : IEntityTypeConfiguration<PricingPlan>
{
    public void Configure(EntityTypeBuilder<PricingPlan> builder)
    {
        builder.ToTable("PricingPlans");
        builder.HasKey(plan => plan.Id);
        builder.Property(plan => plan.Code).HasMaxLength(80).IsRequired();
        builder.Property(plan => plan.DisplayName).HasMaxLength(120).IsRequired();
        builder.Property(plan => plan.Description).HasMaxLength(500).IsRequired();
        builder.Property(plan => plan.CustomerType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(plan => plan.BillingPeriod).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(plan => plan.Price).HasPrecision(18, 2).IsRequired();
        builder.Property(plan => plan.BookingCommissionRate).HasPrecision(5, 2).IsRequired();
        builder.Property(plan => plan.CurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(plan => plan.AiItineraryGenerationsPerPeriod).IsRequired();
        builder.Property(plan => plan.MaxPublishedPackages).IsRequired();
        builder.Property(plan => plan.BenefitsText).HasMaxLength(2000).IsRequired();
        builder.Property(plan => plan.IsActive).HasDefaultValue(true).IsRequired();
        builder.Property(plan => plan.CreatedAt).IsRequired();
        builder.HasIndex(plan => plan.Code).IsUnique();
        builder.HasIndex(plan => new { plan.CustomerType, plan.BillingPeriod, plan.IsActive });
    }
}
