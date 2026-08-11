using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Safarni.Domain.Entities;

namespace Safarni.Infrastructure.Persistence.Configurations;

public sealed class PackageBookingRequestConfiguration : IEntityTypeConfiguration<PackageBookingRequest>
{
    public void Configure(EntityTypeBuilder<PackageBookingRequest> builder)
    {
        builder.ToTable("PackageBookingRequests");
        builder.HasKey(request => request.Id);
        builder.Property(request => request.Message).HasMaxLength(2000);
        builder.Property(request => request.AgencyResponse).HasMaxLength(2000);
        builder.Property(request => request.ChildrenAgesJson).HasMaxLength(500).HasDefaultValue("[]").IsRequired();
        builder.Property(request => request.ContactPhone).HasMaxLength(40).HasDefaultValue("Not provided").IsRequired();
        builder.Property(request => request.AlternatePhone).HasMaxLength(40);
        builder.Property(request => request.TravelerCountry).HasMaxLength(100).HasDefaultValue("Not provided").IsRequired();
        builder.Property(request => request.TravelerCity).HasMaxLength(100).HasDefaultValue("Not provided").IsRequired();
        builder.Property(request => request.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(request => request.CreatedAt).IsRequired();
        builder.HasOne(request => request.TravelPackage)
            .WithMany(package => package.BookingRequests)
            .HasForeignKey(request => request.TravelPackageId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(request => request.TravelerUser)
            .WithMany()
            .HasForeignKey(request => request.TravelerUserId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasIndex(request => new { request.TravelPackageId, request.Status });
        builder.HasIndex(request => request.TravelerUserId);
    }
}
