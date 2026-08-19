using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Safarni.Domain.Entities;

namespace Safarni.Infrastructure.Persistence.Configurations;

public sealed class PackageBookingDocumentConfiguration : IEntityTypeConfiguration<PackageBookingDocument>
{
    public void Configure(EntityTypeBuilder<PackageBookingDocument> builder)
    {
        builder.ToTable("PackageBookingDocuments");
        builder.HasKey(document => document.Id);
        builder.Property(document => document.DocumentType).HasMaxLength(100).IsRequired();
        builder.Property(document => document.StorageReference).HasMaxLength(500).IsRequired();
        builder.Property(document => document.OriginalFileName).HasMaxLength(255).IsRequired();
        builder.Property(document => document.ContentType).HasMaxLength(120).IsRequired();
        builder.HasIndex(document => new { document.PackageBookingRequestId, document.CreatedAt });
        builder.HasOne(document => document.PackageBookingRequest)
            .WithMany(request => request.Documents)
            .HasForeignKey(document => document.PackageBookingRequestId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(document => document.PackageBookingTraveler)
            .WithMany(traveler => traveler.Documents)
            .HasForeignKey(document => document.PackageBookingTravelerId)
            // Avoid a SQL Server multiple-cascade-path conflict through the booking request.
            .OnDelete(DeleteBehavior.NoAction);
    }
}
