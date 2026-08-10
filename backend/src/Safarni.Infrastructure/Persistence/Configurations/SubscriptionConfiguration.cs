using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Safarni.Domain.Entities;

namespace Safarni.Infrastructure.Persistence.Configurations;

public sealed class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("Subscriptions");
        builder.HasKey(subscription => subscription.Id);
        builder.Property(subscription => subscription.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(subscription => subscription.ProviderSubscriptionId).HasMaxLength(200);
        builder.Property(subscription => subscription.CancelAtPeriodEnd).IsRequired();
        builder.Property(subscription => subscription.CreatedAt).IsRequired();
        builder.HasOne(subscription => subscription.Plan)
            .WithMany(plan => plan.Subscriptions)
            .HasForeignKey(subscription => subscription.PlanId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(subscription => subscription.User)
            .WithMany(user => user.Subscriptions)
            .HasForeignKey(subscription => subscription.UserId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(subscription => subscription.TravelAgency)
            .WithMany(agency => agency.Subscriptions)
            .HasForeignKey(subscription => subscription.TravelAgencyId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasIndex(subscription => new { subscription.UserId, subscription.Status });
        builder.HasIndex(subscription => new { subscription.TravelAgencyId, subscription.Status });
        builder.HasIndex(subscription => subscription.ProviderSubscriptionId)
            .IsUnique()
            .HasFilter("[ProviderSubscriptionId] IS NOT NULL");
    }
}
