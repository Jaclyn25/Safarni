using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripMindAI.Domain.Entities;

namespace TripMindAI.Infrastructure.Data.Configurations;

public class TripWeatherForecastConfiguration : IEntityTypeConfiguration<TripWeatherForecast>
{
    public void Configure(EntityTypeBuilder<TripWeatherForecast> builder)
    {
        builder.HasKey(w => w.Id);

        builder.Property(w => w.LocationName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(w => w.WeatherDescription)
            .HasMaxLength(500);

        builder.Property(w => w.TemperatureCelsius)
            .HasPrecision(5, 2);

        builder.Property(w => w.RainProbability)
            .HasPrecision(5, 2);

        builder.Property(w => w.WindSpeed)
            .HasPrecision(5, 2);

        builder.Property(w => w.Provider)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(w => w.RawResponseJson)
            .HasMaxLength(8000);

        builder.Property(w => w.RetrievedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(w => w.TripDay)
            .WithOne(d => d.WeatherForecast)
            .HasForeignKey<TripWeatherForecast>(w => w.TripDayId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
