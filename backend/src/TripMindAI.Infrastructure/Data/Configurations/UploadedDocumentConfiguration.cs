using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripMindAI.Domain.Entities;

namespace TripMindAI.Infrastructure.Data.Configurations;

public class UploadedDocumentConfiguration : IEntityTypeConfiguration<UploadedDocument>
{
    public void Configure(EntityTypeBuilder<UploadedDocument> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.DocumentType)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(d => d.OriginalFileName)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(d => d.FilePath)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(d => d.MimeType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(d => d.UploadStatus)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(d => d.AnalysisStatus)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(d => d.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(d => d.User)
            .WithMany(u => u.UploadedDocuments)
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.VisaApplication)
            .WithMany(v => v.UploadedDocuments)
            .HasForeignKey(d => d.VisaApplicationId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(d => d.AnalysisResult)
            .WithOne(a => a.UploadedDocument)
            .HasForeignKey<DocumentAnalysisResult>(a => a.UploadedDocumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
