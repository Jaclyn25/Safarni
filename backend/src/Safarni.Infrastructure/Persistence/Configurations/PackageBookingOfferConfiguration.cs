using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Safarni.Domain.Entities;

namespace Safarni.Infrastructure.Persistence.Configurations;

public sealed class PackageBookingOfferConfiguration : IEntityTypeConfiguration<PackageBookingOffer>
{
    public void Configure(EntityTypeBuilder<PackageBookingOffer> builder)
    {
        builder.ToTable("PackageBookingOffers");
        builder.HasKey(offer => offer.Id);
        builder.Property(offer => offer.CurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(offer => offer.Included).HasMaxLength(4000);
        builder.Property(offer => offer.Excluded).HasMaxLength(4000);
        builder.Property(offer => offer.CancellationPolicy).HasMaxLength(4000);
        builder.Property(offer => offer.AgencyNote).HasMaxLength(2000);
        builder.Property(offer => offer.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(offer => offer.Amount).HasPrecision(18, 2);
        builder.Property(offer => offer.SupplierCostAmount).HasPrecision(18, 2);
        builder.Property(offer => offer.AgencyServiceFeeAmount).HasPrecision(18, 2);
        builder.Property(offer => offer.SafarniProtectionFeeAmount).HasPrecision(18, 2);
        builder.Property(offer => offer.InitialPaymentPercentage).HasPrecision(5, 2).IsRequired();
        builder.Property(offer => offer.CreatedAt).IsRequired();
        builder.Property(offer => offer.ExpiresAt).IsRequired();
        builder.HasOne(offer => offer.PackageBookingRequest)
            .WithMany(request => request.Offers)
            .HasForeignKey(offer => offer.PackageBookingRequestId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(offer => new { offer.PackageBookingRequestId, offer.Version }).IsUnique();
        builder.HasIndex(offer => new { offer.PackageBookingRequestId, offer.Status, offer.ExpiresAt });
    }
}
