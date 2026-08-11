using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Safarni.Domain.Entities;

namespace Safarni.Infrastructure.Persistence.Configurations;

public sealed class TravelPackageItemConfiguration : IEntityTypeConfiguration<TravelPackageItem>
{
    public void Configure(EntityTypeBuilder<TravelPackageItem> builder)
    {
        builder.ToTable("TravelPackageItems");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Type).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(item => item.Source).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(item => item.Title).HasMaxLength(200).IsRequired();
        builder.Property(item => item.Description).HasMaxLength(3000);
        builder.Property(item => item.LocationName).HasMaxLength(250);
        builder.Property(item => item.Address).HasMaxLength(500);
        builder.Property(item => item.MapUrl).HasMaxLength(1000);
        builder.Property(item => item.Provider).HasMaxLength(100);
        builder.Property(item => item.ProviderPlaceId).HasMaxLength(500);
        builder.Property(item => item.ProviderPhotoUrl).HasMaxLength(1000);
        builder.Property(item => item.WebsiteUrl).HasMaxLength(1000);
        builder.Property(item => item.Cuisine).HasMaxLength(150);
        builder.Property(item => item.MealType).HasMaxLength(100);
        builder.Property(item => item.AccommodationType).HasMaxLength(100);
        builder.Property(item => item.RoomType).HasMaxLength(150);
        builder.Property(item => item.BoardBasis).HasMaxLength(150);
        builder.Property(item => item.TransportMode).HasMaxLength(100);
        builder.Property(item => item.FromLocation).HasMaxLength(250);
        builder.Property(item => item.ToLocation).HasMaxLength(250);
        builder.Property(item => item.MeetingPoint).HasMaxLength(500);
        builder.Property(item => item.DifficultyLevel).HasMaxLength(80);
        builder.Property(item => item.AdditionalDetails).HasMaxLength(3000);
        builder.HasOne(item => item.TravelPackageDay)
            .WithMany(day => day.Items)
            .HasForeignKey(item => item.TravelPackageDayId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(item => new { item.TravelPackageDayId, item.OrderIndex });
        builder.HasIndex(item => new { item.Provider, item.ProviderPlaceId });
    }
}
