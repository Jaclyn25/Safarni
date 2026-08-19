using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Safarni.Domain.Entities;

namespace Safarni.Infrastructure.Persistence.Configurations;

public sealed class PackageBookingTravelerConfiguration : IEntityTypeConfiguration<PackageBookingTraveler>
{
    public void Configure(EntityTypeBuilder<PackageBookingTraveler> builder)
    {
        builder.ToTable("PackageBookingTravelers");
        builder.HasKey(traveler => traveler.Id);
        builder.Property(traveler => traveler.FullName).HasMaxLength(200).IsRequired();
        builder.Property(traveler => traveler.Nationality).HasMaxLength(100).IsRequired();
        builder.Property(traveler => traveler.PassportNumber).HasMaxLength(100).IsRequired();
        builder.Property(traveler => traveler.Email).HasMaxLength(320).IsRequired();
        builder.Property(traveler => traveler.Phone).HasMaxLength(40).IsRequired();
        builder.HasIndex(traveler => traveler.PackageBookingRequestId);
        builder.HasOne(traveler => traveler.PackageBookingRequest)
            .WithMany(request => request.Travelers)
            .HasForeignKey(traveler => traveler.PackageBookingRequestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
