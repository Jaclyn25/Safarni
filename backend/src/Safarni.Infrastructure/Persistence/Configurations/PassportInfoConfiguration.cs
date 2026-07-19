using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Safarni.Domain.Entities;

namespace Safarni.Infrastructure.Persistence.Configurations;

public sealed class PassportInfoConfiguration : IEntityTypeConfiguration<PassportInfo>
{
    public void Configure(EntityTypeBuilder<PassportInfo> builder)
    {
        builder.ToTable("PassportInfos");

        builder.HasKey(passportInfo => passportInfo.Id);

        builder.Property(passportInfo => passportInfo.PassportNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(passportInfo => passportInfo.Nationality)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(passportInfo => passportInfo.ExpiryDate)
            .IsRequired();

        builder.Property(passportInfo => passportInfo.UpdatedAt)
            .IsRequired();

        builder.HasOne(passportInfo => passportInfo.User)
            .WithOne(user => user.PassportInfo)
            .HasForeignKey<PassportInfo>(passportInfo => passportInfo.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(passportInfo => passportInfo.UserId)
            .IsUnique();
    }
}
