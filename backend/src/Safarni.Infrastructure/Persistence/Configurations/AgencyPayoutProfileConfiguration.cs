using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Safarni.Domain.Entities;

namespace Safarni.Infrastructure.Persistence.Configurations;

public sealed class AgencyPayoutProfileConfiguration : IEntityTypeConfiguration<AgencyPayoutProfile>
{
    public void Configure(EntityTypeBuilder<AgencyPayoutProfile> builder)
    {
        builder.ToTable("AgencyPayoutProfiles");
        builder.HasKey(profile => profile.Id);
        builder.Property(profile => profile.Method).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(profile => profile.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(profile => profile.AccountHolderName).HasMaxLength(200).IsRequired();
        builder.Property(profile => profile.Country).HasMaxLength(100).IsRequired();
        builder.Property(profile => profile.BankName).HasMaxLength(200);
        builder.Property(profile => profile.Iban).HasMaxLength(100);
        builder.Property(profile => profile.AccountNumber).HasMaxLength(100);
        builder.Property(profile => profile.SwiftCode).HasMaxLength(20);
        builder.Property(profile => profile.WalletProvider).HasMaxLength(100);
        builder.Property(profile => profile.WalletNumber).HasMaxLength(100);
        builder.HasIndex(profile => profile.TravelAgencyId).IsUnique();
        builder.HasOne(profile => profile.TravelAgency)
            .WithOne(agency => agency.PayoutProfile)
            .HasForeignKey<AgencyPayoutProfile>(profile => profile.TravelAgencyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
