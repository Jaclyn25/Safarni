using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Safarni.Domain.Entities;

namespace Safarni.Infrastructure.Persistence.Configurations;

public sealed class AgencyVerificationDocumentConfiguration
    : IEntityTypeConfiguration<AgencyVerificationDocument>
{
    public void Configure(
        EntityTypeBuilder<AgencyVerificationDocument> builder)
    {
        builder.ToTable("AgencyVerificationDocuments");

        builder.HasKey(document => document.Id);

        builder.Property(document => document.DocumentType)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(document => document.DocumentNumber)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(document => document.StorageReference)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(document => document.OriginalFileName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(document => document.ContentType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(document => document.FileSizeBytes)
            .IsRequired();

        builder.Property(document => document.UploadedAt)
            .IsRequired();

        builder.HasOne(document => document.TravelAgency)
            .WithMany(agency => agency.VerificationDocuments)
            .HasForeignKey(document => document.TravelAgencyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(document => new
            {
                document.TravelAgencyId,
                document.DocumentType
            })
            .IsUnique();
    }
}
