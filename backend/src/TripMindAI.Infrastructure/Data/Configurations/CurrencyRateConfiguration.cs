using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripMindAI.Domain.Entities;

namespace TripMindAI.Infrastructure.Data.Configurations;

public class CurrencyRateConfiguration : IEntityTypeConfiguration<CurrencyRate>
{
    public void Configure(EntityTypeBuilder<CurrencyRate> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.BaseCurrency)
            .HasMaxLength(3)
            .HasDefaultValue("USD");

        builder.Property(c => c.TargetCurrency)
            .HasMaxLength(3)
            .HasDefaultValue("EGP");

        builder.Property(c => c.Rate)
            .HasPrecision(18, 6);

        builder.Property(c => c.Provider)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(c => c.RetrievedAt)
            .HasDefaultValueSql("GETUTCDATE()");
    }
}
