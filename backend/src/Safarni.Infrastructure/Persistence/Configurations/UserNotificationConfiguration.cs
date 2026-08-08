using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Safarni.Domain.Entities;

namespace Safarni.Infrastructure.Persistence.Configurations;

public sealed class UserNotificationConfiguration
    : IEntityTypeConfiguration<UserNotification>
{
    public void Configure(EntityTypeBuilder<UserNotification> builder)
    {
        builder.ToTable("UserNotifications");
        builder.HasKey(notification => notification.Id);

        builder.Property(notification => notification.Type)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();
        builder.Property(notification => notification.Title)
            .HasMaxLength(160)
            .IsRequired();
        builder.Property(notification => notification.Message)
            .HasMaxLength(1000)
            .IsRequired();
        builder.Property(notification => notification.ActionUrl)
            .HasMaxLength(500);

        builder.HasOne(notification => notification.RecipientUser)
            .WithMany(user => user.Notifications)
            .HasForeignKey(notification => notification.RecipientUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(notification => new
        {
            notification.RecipientUserId,
            notification.IsRead,
            notification.CreatedAt
        });
    }
}
