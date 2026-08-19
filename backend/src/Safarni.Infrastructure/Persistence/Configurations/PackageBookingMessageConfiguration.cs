using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Safarni.Domain.Entities;

namespace Safarni.Infrastructure.Persistence.Configurations;

public sealed class PackageBookingMessageConfiguration : IEntityTypeConfiguration<PackageBookingMessage>
{
    public void Configure(EntityTypeBuilder<PackageBookingMessage> builder)
    {
        builder.ToTable("PackageBookingMessages");
        builder.HasKey(message => message.Id);
        builder.Property(message => message.Body).HasMaxLength(4000).IsRequired();
        builder.HasIndex(message => new { message.PackageBookingRequestId, message.CreatedAt });
        builder.HasOne(message => message.PackageBookingRequest)
            .WithMany(request => request.Messages)
            .HasForeignKey(message => message.PackageBookingRequestId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(message => message.SenderUser)
            .WithMany()
            .HasForeignKey(message => message.SenderUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
