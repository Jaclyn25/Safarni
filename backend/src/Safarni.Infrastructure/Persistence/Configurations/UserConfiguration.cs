using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Safarni.Domain.Entities;

namespace Safarni.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(user => user.Id);

        builder.Property(user => user.FullName)
            .HasMaxLength(100)
            .IsRequired();
        builder.Property(user => user.PhoneNumber)
          .HasMaxLength(20);

        builder.Property(user => user.DateOfBirth);

        builder.Property(user => user.Country)
            .HasMaxLength(100);

        builder.Property(user => user.City)
            .HasMaxLength(100);

        builder.Property(user => user.Bio)
            .HasMaxLength(500);

        builder.Property(user => user.Email)
            .HasMaxLength(256)
            .IsRequired();

        builder.HasIndex(user => user.Email)
            .IsUnique();

        builder.Property(user => user.ProfilePhotoUrl)
           .HasMaxLength(500);
        builder.Property(user => user.PasswordHash)
            .IsRequired();

        builder.Property(user => user.CreatedAt)
            .IsRequired();
        builder.Property(user => user.UpdatedAt);

        builder.Property(user => user.IsActive)
            .HasDefaultValue(true)
            .IsRequired();
    }
}