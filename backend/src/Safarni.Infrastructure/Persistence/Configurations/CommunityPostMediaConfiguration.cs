using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Safarni.Domain.Entities;

namespace Safarni.Infrastructure.Persistence.Configurations;

public sealed class CommunityPostMediaConfiguration : IEntityTypeConfiguration<CommunityPostMedia>
{
    public void Configure(EntityTypeBuilder<CommunityPostMedia> builder)
    {
        builder.ToTable("CommunityPostMedia");
        builder.HasKey(media => media.Id);

        builder.Property(media => media.MediaType).HasMaxLength(20).IsRequired();
        builder.Property(media => media.Url).HasMaxLength(1000).IsRequired();
        builder.Property(media => media.ThumbnailUrl).HasMaxLength(1000);
        builder.Property(media => media.AltText).HasMaxLength(240);

        builder.HasOne(media => media.Post)
            .WithMany(post => post.Media)
            .HasForeignKey(media => media.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(media => new { media.PostId, media.SortOrder });
    }
}
