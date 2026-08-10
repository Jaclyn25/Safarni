using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Safarni.Domain.Entities;

namespace Safarni.Infrastructure.Persistence.Configurations;

public sealed class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>
{
    public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
    {
        builder.ToTable("PaymentTransactions");
        builder.HasKey(payment => payment.Id);
        builder.Property(payment => payment.Provider).HasMaxLength(40).IsRequired();
        builder.Property(payment => payment.PaymentSessionId).HasMaxLength(200).IsRequired();
        builder.Property(payment => payment.ProviderEventId).HasMaxLength(200);
        builder.Property(payment => payment.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(payment => payment.CurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(payment => payment.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(payment => payment.FailureReason).HasMaxLength(1000);
        builder.Property(payment => payment.CreatedAt).IsRequired();
        builder.HasOne(payment => payment.Subscription)
            .WithMany(subscription => subscription.Payments)
            .HasForeignKey(payment => payment.SubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(payment => new { payment.Provider, payment.PaymentSessionId }).IsUnique();
        builder.HasIndex(payment => payment.ProviderEventId).IsUnique().HasFilter("[ProviderEventId] IS NOT NULL");
    }
}
