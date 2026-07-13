using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Safarni.Domain.Entities;

namespace Safarni.Infrastructure.Persistence.Configurations;

public sealed class VisaApplicationConfiguration : IEntityTypeConfiguration<VisaApplication>
{
    public void Configure(EntityTypeBuilder<VisaApplication> builder)
    {
        builder.ToTable("VisaApplications");

        builder.HasKey(application => application.Id);

        builder.Property(application => application.DestinationCountry)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(application => application.TravelPurpose)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(application => application.PlannedTravelDate)
            .IsRequired();

        builder.Property(application => application.VisaType)
            .HasMaxLength(100);

        builder.Property(application => application.Status)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(application => application.CreatedAt)
            .IsRequired();

        builder.Property(application => application.UpdatedAt)
            .IsRequired();

        builder.HasOne(application => application.User)
            .WithMany(user => user.VisaApplications)
            .HasForeignKey(application => application.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(application => application.UserId);
    }
}
