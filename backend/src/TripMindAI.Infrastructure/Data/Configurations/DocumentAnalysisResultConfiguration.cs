using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripMindAI.Domain.Entities;

namespace TripMindAI.Infrastructure.Data.Configurations;

public class DocumentAnalysisResultConfiguration : IEntityTypeConfiguration<DocumentAnalysisResult>
{
    public void Configure(EntityTypeBuilder<DocumentAnalysisResult> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.AiProvider)
            .HasMaxLength(50)
            .HasDefaultValue("Gemini");

        builder.Property(r => r.ModelName)
            .HasMaxLength(100);

        builder.Property(r => r.ConfidenceScore)
            .HasPrecision(5, 4);

        builder.Property(r => r.ExtractedDataJson)
            .HasMaxLength(8000)
            .HasDefaultValue("{}");

        builder.Property(r => r.QualityIssuesJson)
            .HasMaxLength(4000)
            .HasDefaultValue("[]");

        builder.Property(r => r.RawAiResponseJson)
            .HasMaxLength(8000)
            .HasDefaultValue("{}");

        builder.Property(r => r.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(r => r.UploadedDocument)
            .WithOne(d => d.AnalysisResult)
            .HasForeignKey<DocumentAnalysisResult>(r => r.UploadedDocumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
