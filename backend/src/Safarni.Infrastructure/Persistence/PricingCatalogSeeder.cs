using Microsoft.EntityFrameworkCore;
using Safarni.Domain.Entities;
using Safarni.Domain.Enums;

namespace Safarni.Infrastructure.Persistence;

public sealed class PricingCatalogSeeder
{
    private readonly ApplicationDbContext _dbContext;

    public PricingCatalogSeeder(ApplicationDbContext dbContext) => _dbContext = dbContext;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var plans = new[]
        {
            Plan("traveler-monthly", "Traveler Premium Monthly", "More AI itinerary planning for flexible trips.", PricingCustomerType.Traveler, BillingPeriod.Monthly, 5m, 10, 0, 10m, "Unlock premium AI planning;10 AI itinerary generations per month;Keep your trip planning flowing"),
            Plan("traveler-yearly", "Traveler Premium Yearly", "A full year of premium AI planning at a lower effective price.", PricingCustomerType.Traveler, BillingPeriod.Yearly, 50m, 120, 0, 10m, "Unlock premium AI planning;120 AI itinerary generations per year;Save versus monthly billing"),
            Plan("agency-monthly", "Agency Marketplace Monthly", "Paid tools for agencies publishing on the Safarni marketplace.", PricingCustomerType.Agency, BillingPeriod.Monthly, 29m, 0, 10, 7m, "Publish up to 10 marketplace packages;Booking commission reduced to 7%;Manage your agency presence;Reach verified travelers"),
            Plan("agency-yearly", "Agency Marketplace Yearly", "A full year of marketplace access for growing agencies.", PricingCustomerType.Agency, BillingPeriod.Yearly, 299m, 0, 100, 5m, "Publish up to 100 marketplace packages;Booking commission reduced to 5%;Manage your agency presence;Save versus monthly billing")
        };

        foreach (var plan in plans)
        {
            var existing = await _dbContext.PricingPlans.FirstOrDefaultAsync(value => value.Code == plan.Code, cancellationToken);
            if (existing is null)
            {
                await _dbContext.PricingPlans.AddAsync(plan, cancellationToken);
            }
            else
            {
                existing.DisplayName = plan.DisplayName;
                existing.Description = plan.Description;
                existing.CustomerType = plan.CustomerType;
                existing.BillingPeriod = plan.BillingPeriod;
                existing.Price = plan.Price;
                existing.BookingCommissionRate = plan.BookingCommissionRate;
                existing.CurrencyCode = plan.CurrencyCode;
                existing.AiItineraryGenerationsPerPeriod = plan.AiItineraryGenerationsPerPeriod;
                existing.MaxPublishedPackages = plan.MaxPublishedPackages;
                existing.BenefitsText = plan.BenefitsText;
                existing.IsActive = true;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static PricingPlan Plan(string code, string displayName, string description, PricingCustomerType customerType, BillingPeriod billingPeriod, decimal price, int aiGenerations, int maxPublishedPackages, decimal bookingCommissionRate, string benefits) => new()
    {
        Id = Guid.NewGuid(), Code = code, DisplayName = displayName, Description = description,
        CustomerType = customerType, BillingPeriod = billingPeriod, Price = price, CurrencyCode = "USD", BookingCommissionRate = bookingCommissionRate,
        AiItineraryGenerationsPerPeriod = aiGenerations, MaxPublishedPackages = maxPublishedPackages,
        BenefitsText = benefits, IsActive = true, CreatedAt = DateTime.UtcNow
    };
}
