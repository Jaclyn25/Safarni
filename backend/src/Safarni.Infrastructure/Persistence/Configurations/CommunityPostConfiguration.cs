using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Safarni.Domain.Entities;

namespace Safarni.Infrastructure.Persistence.Configurations;

public sealed class CommunityPostConfiguration : IEntityTypeConfiguration<CommunityPost>
{
    public void Configure(EntityTypeBuilder<CommunityPost> builder)
    {
        builder.ToTable("CommunityPosts");
        builder.HasKey(post => post.Id);

        builder.Property(post => post.Type).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(post => post.Status).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(post => post.Title).HasMaxLength(180).IsRequired();
        builder.Property(post => post.Body).HasMaxLength(12000).IsRequired();
        builder.Property(post => post.Destination).HasMaxLength(160);

        builder.HasOne(post => post.Author)
            .WithMany()
            .HasForeignKey(post => post.AuthorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(post => post.LinkedTrip)
            .WithMany()
            .HasForeignKey(post => post.LinkedTripId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(post => new { post.Status, post.CreatedAt });
        builder.HasIndex(post => post.Destination);
    }
}
