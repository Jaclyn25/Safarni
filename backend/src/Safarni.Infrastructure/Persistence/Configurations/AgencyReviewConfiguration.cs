using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Safarni.Domain.Entities;

namespace Safarni.Infrastructure.Persistence.Configurations;

public sealed class AgencyReviewConfiguration : IEntityTypeConfiguration<AgencyReview>
{
    public void Configure(EntityTypeBuilder<AgencyReview> builder)
    {
        builder.ToTable("AgencyReviews");
        builder.HasKey(review => review.Id);
        builder.Property(review => review.Comment).HasMaxLength(3000).IsRequired();
        builder.Property(review => review.AgencyResponse).HasMaxLength(2000);
        builder.Property(review => review.CreatedAt).IsRequired();
        builder.HasOne(review => review.TravelAgency)
            .WithMany(agency => agency.Reviews)
            .HasForeignKey(review => review.TravelAgencyId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(review => review.TravelerUser)
            .WithMany()
            .HasForeignKey(review => review.TravelerUserId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(review => review.PackageBookingRequest)
            .WithOne(request => request.AgencyReview)
            .HasForeignKey<AgencyReview>(review => review.PackageBookingRequestId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasIndex(review => review.PackageBookingRequestId).IsUnique();
        builder.HasIndex(review => new { review.TravelAgencyId, review.IsPublished, review.CreatedAt });
    }
}
