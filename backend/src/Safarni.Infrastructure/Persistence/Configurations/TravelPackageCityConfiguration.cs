using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Safarni.Domain.Entities;

namespace Safarni.Infrastructure.Persistence.Configurations;

public sealed class TravelPackageCityConfiguration : IEntityTypeConfiguration<TravelPackageCity>
{
    public void Configure(EntityTypeBuilder<TravelPackageCity> builder)
    {
        builder.ToTable("TravelPackageCities");
        builder.HasKey(city => city.Id);
        builder.Property(city => city.Name).HasMaxLength(150).IsRequired();
        builder.Property(city => city.Description).HasMaxLength(1000);
        builder.HasOne(city => city.TravelPackage)
            .WithMany(package => package.Cities)
            .HasForeignKey(city => city.TravelPackageId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(city => new { city.TravelPackageId, city.OrderIndex }).IsUnique();
    }
}
