using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Safarni.Domain.Entities;

namespace Safarni.Infrastructure.Persistence.Configurations;

public sealed class UserSettingsConfiguration : IEntityTypeConfiguration<UserSettings>
{
    public void Configure(EntityTypeBuilder<UserSettings> builder)
    {
        builder.ToTable("UserSettings");

        builder.HasKey(settings => settings.Id);

        builder.Property(settings => settings.UserId)
            .IsRequired();

        builder.HasIndex(settings => settings.UserId)
            .IsUnique();

        builder.Property(settings => settings.EmailNotificationsEnabled)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(settings => settings.PushNotificationsEnabled)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(settings => settings.ProfileIsPrivate)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(settings => settings.ShowLastSeen)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(settings => settings.ShowEmail)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(settings => settings.ShowPhoneNumber)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(settings => settings.CreatedAt)
            .IsRequired();

        builder.Property(settings => settings.UpdatedAt);

        builder.HasOne(settings => settings.User)
            .WithOne()
            .HasForeignKey<UserSettings>(settings => settings.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
