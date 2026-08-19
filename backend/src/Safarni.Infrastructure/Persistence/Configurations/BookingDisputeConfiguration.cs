using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Safarni.Domain.Entities;

namespace Safarni.Infrastructure.Persistence.Configurations;

public sealed class BookingDisputeConfiguration : IEntityTypeConfiguration<BookingDispute>
{
    public void Configure(EntityTypeBuilder<BookingDispute> builder)
    {
        builder.ToTable("BookingDisputes");
        builder.HasKey(dispute => dispute.Id);
        builder.Property(dispute => dispute.Reason).HasMaxLength(2000).IsRequired();
        builder.Property(dispute => dispute.AdminNote).HasMaxLength(2000);
        builder.Property(dispute => dispute.RequestedRefundAmount).HasPrecision(18, 2);
        builder.Property(dispute => dispute.ResolvedRefundAmount).HasPrecision(18, 2);
        builder.Property(dispute => dispute.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.HasIndex(dispute => new { dispute.Status, dispute.CreatedAt });
        builder.HasOne(dispute => dispute.PackageBookingRequest)
            .WithMany(request => request.Disputes)
            .HasForeignKey(dispute => dispute.PackageBookingRequestId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(dispute => dispute.OpenedByUser)
            .WithMany()
            .HasForeignKey(dispute => dispute.OpenedByUserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
