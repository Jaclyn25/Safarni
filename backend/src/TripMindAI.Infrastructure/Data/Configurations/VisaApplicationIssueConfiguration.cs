using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripMindAI.Domain.Entities;

namespace TripMindAI.Infrastructure.Data.Configurations;

public class VisaApplicationIssueConfiguration : IEntityTypeConfiguration<VisaApplicationIssue>
{
    public void Configure(EntityTypeBuilder<VisaApplicationIssue> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.IssueType)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(i => i.Severity)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(i => i.Title)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(i => i.Message)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(i => i.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(i => i.VisaApplication)
            .WithMany(v => v.Issues)
            .HasForeignKey(i => i.VisaApplicationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
