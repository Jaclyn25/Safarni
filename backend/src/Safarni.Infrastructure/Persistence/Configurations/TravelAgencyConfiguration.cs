using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Safarni.Domain.Entities;
using Safarni.Domain.Enums;

namespace Safarni.Infrastructure.Persistence.Configurations;

public sealed class TravelAgencyConfiguration
    : IEntityTypeConfiguration<TravelAgency>
{
    public void Configure(EntityTypeBuilder<TravelAgency> builder)
    {
        builder.ToTable("TravelAgencies");

        builder.HasKey(agency => agency.Id);

        builder.Property(agency => agency.CompanyName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(agency => agency.Description)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(agency => agency.Address)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(agency => agency.City)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(agency => agency.Country)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(agency => agency.CompanyPhone)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(agency => agency.LogoUrl)
            .HasMaxLength(500);

        builder.Property(agency => agency.WebsiteUrl)
            .HasMaxLength(500);

        builder.Property(agency => agency.VerificationStatus)
            .HasConversion<string>()
            .HasMaxLength(30)
            .HasDefaultValue(AgencyVerificationStatus.Pending)
            .IsRequired();

        builder.Property(agency => agency.VerificationDecisionReason)
            .HasMaxLength(1000);

        builder.Property(agency => agency.CreatedAt)
            .IsRequired();

        builder.HasOne(agency => agency.User)
            .WithOne(user => user.TravelAgency)
            .HasForeignKey<TravelAgency>(agency => agency.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(agency => agency.UserId)
            .IsUnique();

        builder.HasIndex(agency => agency.CompanyName)
            .IsUnique();

        builder.HasIndex(agency => agency.VerificationStatus);

        builder.HasIndex(agency => agency.VerificationReviewedByUserId);
    }
}
