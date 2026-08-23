using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Safarni.Domain.Entities;

namespace Safarni.Infrastructure.Persistence.Configurations;

public sealed class CommunityCommentMediaConfiguration : IEntityTypeConfiguration<CommunityCommentMedia>
{
    public void Configure(EntityTypeBuilder<CommunityCommentMedia> builder)
    {
        builder.ToTable("CommunityCommentMedia");
        builder.HasKey(media => media.Id);
        builder.Property(media => media.MediaType).HasMaxLength(20).IsRequired();
        builder.Property(media => media.Url).HasMaxLength(1000).IsRequired();
        builder.Property(media => media.ThumbnailUrl).HasMaxLength(1000);
        builder.Property(media => media.AltText).HasMaxLength(240);

        builder.HasOne(media => media.Comment)
            .WithMany(comment => comment.Media)
            .HasForeignKey(media => media.CommentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(media => new { media.CommentId, media.SortOrder });
    }
}
