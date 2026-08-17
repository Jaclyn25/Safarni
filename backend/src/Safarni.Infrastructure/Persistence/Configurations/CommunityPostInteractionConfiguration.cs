using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Safarni.Domain.Entities;

namespace Safarni.Infrastructure.Persistence.Configurations;

public sealed class CommunityPostInteractionConfiguration : IEntityTypeConfiguration<CommunityPostInteraction>
{
    public void Configure(EntityTypeBuilder<CommunityPostInteraction> builder)
    {
        builder.ToTable("CommunityPostInteractions");
        builder.HasKey(interaction => interaction.Id);
        builder.Property(interaction => interaction.Type).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasOne(interaction => interaction.Post)
            .WithMany(post => post.Interactions)
            .HasForeignKey(interaction => interaction.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(interaction => interaction.User)
            .WithMany()
            .HasForeignKey(interaction => interaction.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(interaction => new
        {
            interaction.UserId,
            interaction.PostId,
            interaction.Type
        }).IsUnique();
    }
}
