using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Safarni.Domain.Entities;

namespace Safarni.Infrastructure.Persistence.Configurations;

public sealed class BookingSupplierEvidenceConfiguration : IEntityTypeConfiguration<BookingSupplierEvidence>
{
    public void Configure(EntityTypeBuilder<BookingSupplierEvidence> builder)
    {
        builder.ToTable("BookingSupplierEvidence");
        builder.HasKey(evidence => evidence.Id);
        builder.Property(evidence => evidence.SupplierType).HasMaxLength(50).IsRequired();
        builder.Property(evidence => evidence.SupplierName).HasMaxLength(200).IsRequired();
        builder.Property(evidence => evidence.ConfirmationCode).HasMaxLength(200).IsRequired();
        builder.Property(evidence => evidence.CurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(evidence => evidence.InvoiceAmount).HasPrecision(18, 2);
        builder.Property(evidence => evidence.StorageReference).HasMaxLength(500).IsRequired();
        builder.Property(evidence => evidence.OriginalFileName).HasMaxLength(255).IsRequired();
        builder.Property(evidence => evidence.ContentType).HasMaxLength(120).IsRequired();
        builder.Property(evidence => evidence.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(evidence => evidence.ReviewNote).HasMaxLength(1000);
        builder.HasIndex(evidence => new { evidence.Status, evidence.CreatedAt });
        builder.HasOne(evidence => evidence.PackageBookingRequest).WithMany(request => request.SupplierEvidence)
            .HasForeignKey(evidence => evidence.PackageBookingRequestId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(evidence => evidence.PackageBookingOffer).WithMany()
            .HasForeignKey(evidence => evidence.PackageBookingOfferId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(evidence => evidence.TravelAgency).WithMany()
            .HasForeignKey(evidence => evidence.TravelAgencyId).OnDelete(DeleteBehavior.Restrict);
    }
}
