using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Safarni.Domain.Entities;

namespace Safarni.Infrastructure.Persistence.Configurations;

public sealed class BookingPayoutConfiguration : IEntityTypeConfiguration<BookingPayout>
{
    public void Configure(EntityTypeBuilder<BookingPayout> builder)
    {
        builder.ToTable("BookingPayouts");
        builder.HasKey(payout => payout.Id);
        builder.Property(payout => payout.Type).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(payout => payout.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(payout => payout.Amount).HasPrecision(18, 2);
        builder.Property(payout => payout.CurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(payout => payout.ExternalTransferId).HasMaxLength(200);
        builder.Property(payout => payout.ReviewNote).HasMaxLength(1000);
        builder.HasIndex(payout => new { payout.Status, payout.ReleaseAt });
        builder.HasIndex(payout => new { payout.Status, payout.CreatedAt });
        builder.HasOne(payout => payout.PackageBookingRequest).WithMany(request => request.Payouts)
            .HasForeignKey(payout => payout.PackageBookingRequestId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(payout => payout.PackageBookingOffer).WithMany()
            .HasForeignKey(payout => payout.PackageBookingOfferId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(payout => payout.TravelAgency).WithMany()
            .HasForeignKey(payout => payout.TravelAgencyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(payout => payout.SupplierEvidence).WithMany()
            .HasForeignKey(payout => payout.SupplierEvidenceId).OnDelete(DeleteBehavior.NoAction);
    }
}
