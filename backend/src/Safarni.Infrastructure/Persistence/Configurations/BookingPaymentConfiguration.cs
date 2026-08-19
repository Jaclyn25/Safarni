using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Safarni.Domain.Entities;

namespace Safarni.Infrastructure.Persistence.Configurations;

public sealed class BookingPaymentConfiguration : IEntityTypeConfiguration<BookingPayment>
{
    public void Configure(EntityTypeBuilder<BookingPayment> builder)
    {
        builder.ToTable("BookingPayments");
        builder.HasKey(payment => payment.Id);
        builder.Property(payment => payment.Provider).HasMaxLength(40).IsRequired();
        builder.Property(payment => payment.PaymentSessionId).HasMaxLength(200).IsRequired();
        builder.Property(payment => payment.ProviderEventId).HasMaxLength(200);
        builder.Property(payment => payment.CurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(payment => payment.GrossAmount).HasPrecision(18, 2);
        builder.Property(payment => payment.CommissionRate).HasPrecision(5, 2);
        builder.Property(payment => payment.CommissionAmount).HasPrecision(18, 2);
        builder.Property(payment => payment.AgencyAmount).HasPrecision(18, 2);
        builder.Property(payment => payment.InstallmentNumber).IsRequired();
        builder.Property(payment => payment.RefundedAmount).HasPrecision(18, 2);
        builder.Property(payment => payment.RefundPercentage).HasPrecision(5, 2);
        builder.Property(payment => payment.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(payment => payment.FailureReason).HasMaxLength(1000);
        builder.Property(payment => payment.ProviderRefundId).HasMaxLength(200);
        builder.HasIndex(payment => new { payment.PackageBookingRequestId, payment.InstallmentNumber });
        builder.HasIndex(payment => new { payment.Provider, payment.PaymentSessionId }).IsUnique();
        builder.HasIndex(payment => new { payment.PackageBookingRequestId, payment.Status });
        builder.HasOne(payment => payment.PackageBookingRequest)
            .WithMany(request => request.Payments)
            .HasForeignKey(payment => payment.PackageBookingRequestId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(payment => payment.PackageBookingOffer)
            .WithMany()
            .HasForeignKey(payment => payment.PackageBookingOfferId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
