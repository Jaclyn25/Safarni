using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripMindAI.Domain.Entities;

namespace TripMindAI.Infrastructure.Data.Configurations;

public class GeneratedReportConfiguration : IEntityTypeConfiguration<GeneratedReport>
{
    public void Configure(EntityTypeBuilder<GeneratedReport> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.ReportType)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(r => r.FilePath)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(r => r.GeneratedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(r => r.User)
            .WithMany(u => u.GeneratedReports)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.TripPlan)
            .WithMany(t => t.GeneratedReports)
            .HasForeignKey(r => r.TripPlanId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(r => r.VisaApplication)
            .WithMany(v => v.GeneratedReports)
            .HasForeignKey(r => r.VisaApplicationId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
