using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Safarni.Domain.Entities;

namespace Safarni.Infrastructure.Persistence.Configurations;

public sealed class CommunityPostCommentConfiguration : IEntityTypeConfiguration<CommunityPostComment>
{
    public void Configure(EntityTypeBuilder<CommunityPostComment> builder)
    {
        builder.ToTable("CommunityPostComments");
        builder.HasKey(comment => comment.Id);
        builder.Property(comment => comment.Body).HasMaxLength(3000).IsRequired();

        builder.HasOne(comment => comment.Post)
            .WithMany(post => post.Comments)
            .HasForeignKey(comment => comment.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(comment => comment.Author)
            .WithMany()
            .HasForeignKey(comment => comment.AuthorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(comment => comment.ParentComment)
            .WithMany(comment => comment.Replies)
            .HasForeignKey(comment => comment.ParentCommentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(comment => new { comment.PostId, comment.CreatedAt });
    }
}
