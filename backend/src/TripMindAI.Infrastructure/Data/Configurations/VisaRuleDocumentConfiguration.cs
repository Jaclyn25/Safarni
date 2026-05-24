using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripMindAI.Domain.Entities;

namespace TripMindAI.Infrastructure.Data.Configurations;

public class VisaRuleDocumentConfiguration : IEntityTypeConfiguration<VisaRuleDocument>
{
    public void Configure(EntityTypeBuilder<VisaRuleDocument> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.DocumentType)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(d => d.Notes)
            .HasMaxLength(500);

        builder.HasOne(d => d.VisaRule)
            .WithMany(v => v.Documents)
            .HasForeignKey(d => d.VisaRuleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
