using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Safarni.Domain.Entities;

namespace Safarni.Infrastructure.Persistence.Configurations;

public sealed class TravelPackageDayConfiguration : IEntityTypeConfiguration<TravelPackageDay>
{
    public void Configure(EntityTypeBuilder<TravelPackageDay> builder)
    {
        builder.ToTable("TravelPackageDays");
        builder.HasKey(day => day.Id);
        builder.Property(day => day.Title).HasMaxLength(200).IsRequired();
        builder.Property(day => day.Description).HasMaxLength(2000).IsRequired();
        builder.HasOne(day => day.TravelPackage)
            .WithMany(package => package.Days)
            .HasForeignKey(day => day.TravelPackageId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(day => day.TravelPackageCity)
            .WithMany(city => city.Days)
            .HasForeignKey(day => day.TravelPackageCityId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasIndex(day => new { day.TravelPackageId, day.DayNumber }).IsUnique();
        builder.HasIndex(day => day.TravelPackageCityId);
    }
}
