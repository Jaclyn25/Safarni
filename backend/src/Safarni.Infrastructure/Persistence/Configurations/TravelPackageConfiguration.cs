using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Safarni.Domain.Entities;

namespace Safarni.Infrastructure.Persistence.Configurations;

public sealed class TravelPackageConfiguration : IEntityTypeConfiguration<TravelPackage>
{
    public void Configure(EntityTypeBuilder<TravelPackage> builder)
    {
        builder.ToTable("TravelPackages");
        builder.HasKey(package => package.Id);
        builder.Property(package => package.Title).HasMaxLength(200).IsRequired();
        builder.Property(package => package.Description).HasMaxLength(4000).IsRequired();
        builder.Property(package => package.Destination).HasMaxLength(200).IsRequired();
        builder.Property(package => package.Price).HasPrecision(18, 2).IsRequired();
        builder.Property(package => package.CurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(package => package.CoverImageUrl).HasMaxLength(1000);
        builder.Property(package => package.DiscountPercentage).HasPrecision(5, 2);
        builder.Property(package => package.OfferLabel).HasMaxLength(200);
        builder.Property(package => package.Inclusions).HasMaxLength(4000);
        builder.Property(package => package.Exclusions).HasMaxLength(4000);
        builder.Property(package => package.ImportantNotes).HasMaxLength(4000);
        builder.Property(package => package.CancellationPolicy).HasMaxLength(4000);
        builder.Property(package => package.IsPublished).HasDefaultValue(false).IsRequired();
        builder.Property(package => package.IsActive).HasDefaultValue(true).IsRequired();
        builder.Property(package => package.CreatedAt).IsRequired();
        builder.HasOne(package => package.TravelAgency)
            .WithMany(agency => agency.Packages)
            .HasForeignKey(package => package.TravelAgencyId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(package => package.TravelAgencyId);
        builder.HasIndex(package => new { package.TravelAgencyId, package.PublishedAt });
        builder.HasIndex(package => new { package.IsPublished, package.IsActive, package.StartDate });
    }
}
