using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Safarni.Domain.Entities;

namespace Safarni.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(refreshToken => refreshToken.Id);

        builder.Property(refreshToken => refreshToken.Token)
            .HasMaxLength(512)
            .IsRequired();

        builder.HasIndex(refreshToken => refreshToken.Token)
            .IsUnique();

        builder.Property(refreshToken => refreshToken.ExpiryDate)
            .IsRequired();

        builder.Property(refreshToken => refreshToken.IsRevoked)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(refreshToken => refreshToken.CreatedAt)
            .IsRequired();

        builder.HasOne(refreshToken => refreshToken.User)
            .WithMany(user => user.RefreshTokens)
            .HasForeignKey(refreshToken => refreshToken.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(refreshToken => refreshToken.UserId);
    }
}