using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Safarni.Domain.Entities;

namespace Safarni.Infrastructure.Persistence.Configurations;

public sealed class TravelPackageImageConfiguration : IEntityTypeConfiguration<TravelPackageImage>
{
    public void Configure(EntityTypeBuilder<TravelPackageImage> builder)
    {
        builder.ToTable("TravelPackageImages");
        builder.HasKey(image => image.Id);
        builder.Property(image => image.Url).HasMaxLength(1000).IsRequired();
        builder.Property(image => image.Caption).HasMaxLength(300);
        builder.Property(image => image.AltText).HasMaxLength(300);
        builder.Property(image => image.CreatedAt).IsRequired();
        builder.HasOne(image => image.TravelPackage)
            .WithMany(package => package.Images)
            .HasForeignKey(image => image.TravelPackageId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(image => image.TravelPackageDay)
            .WithMany(day => day.Images)
            .HasForeignKey(image => image.TravelPackageDayId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(image => image.TravelPackageItem)
            .WithMany(item => item.Images)
            .HasForeignKey(image => image.TravelPackageItemId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasIndex(image => new { image.TravelPackageId, image.SortOrder });
        builder.HasIndex(image => image.TravelPackageDayId);
        builder.HasIndex(image => image.TravelPackageItemId);
    }
}
