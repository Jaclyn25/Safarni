using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripMindAI.Domain.Entities;

namespace TripMindAI.Infrastructure.Data.Configurations;

public class TripDayConfiguration : IEntityTypeConfiguration<TripDay>
{
    public void Configure(EntityTypeBuilder<TripDay> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Title)
            .HasMaxLength(200);

        builder.HasOne(d => d.TripPlan)
            .WithMany(t => t.Days)
            .HasForeignKey(d => d.TripPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(d => d.Activities)
            .WithOne(a => a.TripDay)
            .HasForeignKey(a => a.TripDayId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(d => d.WeatherForecast)
            .WithOne(w => w.TripDay)
            .HasForeignKey<TripWeatherForecast>(w => w.TripDayId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
