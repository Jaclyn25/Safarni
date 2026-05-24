using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TripMindAI.Domain.Entities;
using TripMindAI.Domain.Enums;

namespace TripMindAI.Infrastructure.Data.Seed;

public static class SeedData
{
    public static void Seed(ModelBuilder builder)
    {
        SeedVisaRules(builder);
        SeedDestinations(builder);
        SeedVisaAlerts(builder);
    }

    private static void SeedVisaRules(ModelBuilder builder)
    {
        var franceId = Guid.Parse("11111111-1111-1111-1111-111111111101");
        var germanyId = Guid.Parse("11111111-1111-1111-1111-111111111102");
        var italyId = Guid.Parse("11111111-1111-1111-1111-111111111103");
        var turkeyId = Guid.Parse("11111111-1111-1111-1111-111111111104");
        var uaeId = Guid.Parse("11111111-1111-1111-1111-111111111105");

        builder.Entity<VisaRule>().HasData(
            new VisaRule { Id = franceId, CountryFrom = "Egypt", CountryTo = "France", VisaType = VisaType.Tourism, MinPassportValidityMonths = 6, MinBlankPassportPages = 2, Notes = "Schengen visa required. Biometric appointment needed.", OfficialSourceUrl = "https://france-visas.gouv.fr", LastUpdated = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc) },
            new VisaRule { Id = germanyId, CountryFrom = "Egypt", CountryTo = "Germany", VisaType = VisaType.Tourism, MinPassportValidityMonths = 6, MinBlankPassportPages = 2, Notes = "Schengen visa required. Appointment booking via external service provider.", OfficialSourceUrl = "https://www.auswaertiges-amt.de", LastUpdated = new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc) },
            new VisaRule { Id = italyId, CountryFrom = "Egypt", CountryTo = "Italy", VisaType = VisaType.Tourism, MinPassportValidityMonths = 6, MinBlankPassportPages = 2, Notes = "Schengen visa required. Visa application center in Cairo.", OfficialSourceUrl = "https://italy-visa.com", LastUpdated = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc) },
            new VisaRule { Id = turkeyId, CountryFrom = "Egypt", CountryTo = "Turkey", VisaType = VisaType.Tourism, MinPassportValidityMonths = 6, MinBlankPassportPages = 1, Notes = "E-visa available for Egyptian passport holders.", OfficialSourceUrl = "https://www.evisa.gov.tr", LastUpdated = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc) },
            new VisaRule { Id = uaeId, CountryFrom = "Egypt", CountryTo = "UAE", VisaType = VisaType.Tourism, MinPassportValidityMonths = 6, MinBlankPassportPages = 2, Notes = "Visa on arrival or pre-arranged visa via airline/hotel.", OfficialSourceUrl = "https://u.ae", LastUpdated = new DateTime(2026, 2, 15, 0, 0, 0, DateTimeKind.Utc) }
        );

        var docId = Guid.Parse("22222222-2222-2222-2222-222222222201");
        var docs = new List<VisaRuleDocument>();
        var visaRuleIds = new[] { franceId, germanyId, italyId, turkeyId, uaeId };
        var docTypes = new[] { DocumentType.Passport, DocumentType.BankStatement, DocumentType.TravelInsurance, DocumentType.HotelBooking, DocumentType.FlightReservation };
        var weights = new[] { 35, 20, 15, 10, 10 };
        var index = 0;

        foreach (var ruleId in visaRuleIds)
        {
            for (int i = 0; i < docTypes.Length; i++)
            {
                index++;
                docs.Add(new VisaRuleDocument
                {
                    Id = Guid.Parse($"22222222-2222-2222-2222-22222222{index:D4}"),
                    VisaRuleId = ruleId,
                    DocumentType = docTypes[i],
                    IsRequired = docTypes[i] == DocumentType.Passport,
                    Weight = weights[i],
                    Notes = docTypes[i] == DocumentType.Passport ? "Must be valid for at least 6 months" : null
                });
            }
        }

        builder.Entity<VisaRuleDocument>().HasData(docs);
    }

    private static void SeedDestinations(ModelBuilder builder)
    {
        var parisId = Guid.Parse("33333333-3333-3333-3333-333333333301");
        var romeId = Guid.Parse("33333333-3333-3333-3333-333333333302");
        var istanbulId = Guid.Parse("33333333-3333-3333-3333-333333333303");
        var dubaiId = Guid.Parse("33333333-3333-3333-3333-333333333304");
        var berlinId = Guid.Parse("33333333-3333-3333-3333-333333333305");

        builder.Entity<Destination>().HasData(
            new Destination { Id = parisId, Country = "France", City = "Paris", Description = "The City of Light, known for its art, fashion, and culture.", AverageDailyBudgetUsd = 150m, BestSeason = "Spring (April-June) and Fall (September-October)", Latitude = 48.8566m, Longitude = 2.3522m },
            new Destination { Id = romeId, Country = "Italy", City = "Rome", Description = "The Eternal City, home to ancient ruins and Renaissance art.", AverageDailyBudgetUsd = 120m, BestSeason = "Spring (April-June) and Fall (September-October)", Latitude = 41.9028m, Longitude = 12.4964m },
            new Destination { Id = istanbulId, Country = "Turkey", City = "Istanbul", Description = "A transcontinental city straddling Europe and Asia.", AverageDailyBudgetUsd = 80m, BestSeason = "Spring (April-June) and Fall (September-November)", Latitude = 41.0082m, Longitude = 28.9784m },
            new Destination { Id = dubaiId, Country = "UAE", City = "Dubai", Description = "A futuristic metropolis with luxury shopping and landmarks.", AverageDailyBudgetUsd = 200m, BestSeason = "November to March", Latitude = 25.2048m, Longitude = 55.2708m },
            new Destination { Id = berlinId, Country = "Germany", City = "Berlin", Description = "A city of history, culture, and vibrant nightlife.", AverageDailyBudgetUsd = 110m, BestSeason = "May to September", Latitude = 52.5200m, Longitude = 13.4050m }
        );

        var poiId = Guid.Parse("44444444-4444-4444-4444-444444444401");
        var seedDate = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var pois = new List<PointOfInterest>
        {
            // Paris
            new() { Id = Guid.Parse("44444444-4444-4444-4444-444444444401"), DestinationId = parisId, Name = "Eiffel Tower", Category = PoiCategory.Landmark, Address = "Champ de Mars, 5 Avenue Anatole France, 75007 Paris", Latitude = 48.8584m, Longitude = 2.2945m, Rating = 4.6m, Provider = ExternalProvider.Manual, EstimatedCostUsd = 25m, CreatedAt = seedDate },
            new() { Id = Guid.Parse("44444444-4444-4444-4444-444444444402"), DestinationId = parisId, Name = "Louvre Museum", Category = PoiCategory.Museum, Address = "Rue de Rivoli, 75001 Paris", Latitude = 48.8606m, Longitude = 2.3376m, Rating = 4.7m, Provider = ExternalProvider.Manual, EstimatedCostUsd = 17m, CreatedAt = seedDate },
            new() { Id = Guid.Parse("44444444-4444-4444-4444-444444444403"), DestinationId = parisId, Name = "Café de Flore", Category = PoiCategory.Cafe, Address = "172 Boulevard Saint-Germain, 75006 Paris", Latitude = 48.8540m, Longitude = 2.3324m, Rating = 4.3m, Provider = ExternalProvider.Manual, EstimatedCostUsd = 15m, CreatedAt = seedDate },
            // Rome
            new() { Id = Guid.Parse("44444444-4444-4444-4444-444444444404"), DestinationId = romeId, Name = "Colosseum", Category = PoiCategory.Landmark, Address = "Piazza del Colosseo, 1, 00184 Roma RM", Latitude = 41.8902m, Longitude = 12.4922m, Rating = 4.7m, Provider = ExternalProvider.Manual, EstimatedCostUsd = 16m, CreatedAt = seedDate },
            new() { Id = Guid.Parse("44444444-4444-4444-4444-444444444405"), DestinationId = romeId, Name = "Vatican Museums", Category = PoiCategory.Museum, Address = "Viale Vaticano, 00165 Roma RM", Latitude = 41.9065m, Longitude = 12.4536m, Rating = 4.6m, Provider = ExternalProvider.Manual, EstimatedCostUsd = 20m, CreatedAt = seedDate },
            new() { Id = Guid.Parse("44444444-4444-4444-4444-444444444406"), DestinationId = romeId, Name = "Trevi Fountain", Category = PoiCategory.Landmark, Address = "Piazza di Trevi, 00187 Roma RM", Latitude = 41.9009m, Longitude = 12.4833m, Rating = 4.5m, Provider = ExternalProvider.Manual, EstimatedCostUsd = 0m, CreatedAt = seedDate },
            // Istanbul
            new() { Id = Guid.Parse("44444444-4444-4444-4444-444444444407"), DestinationId = istanbulId, Name = "Hagia Sophia", Category = PoiCategory.Landmark, Address = "Sultan Ahmet Mahallesi, Ayasofya Meydanı, Fatih/İstanbul", Latitude = 41.0086m, Longitude = 28.9802m, Rating = 4.8m, Provider = ExternalProvider.Manual, EstimatedCostUsd = 10m, CreatedAt = seedDate },
            new() { Id = Guid.Parse("44444444-4444-4444-4444-444444444408"), DestinationId = istanbulId, Name = "Grand Bazaar", Category = PoiCategory.Shopping, Address = "Beyazıt, 34126 Fatih/İstanbul", Latitude = 41.0107m, Longitude = 28.9680m, Rating = 4.3m, Provider = ExternalProvider.Manual, EstimatedCostUsd = 0m, CreatedAt = seedDate },
            new() { Id = Guid.Parse("44444444-4444-4444-4444-444444444409"), DestinationId = istanbulId, Name = "Bosphorus Cruise", Category = PoiCategory.Attraction, Address = "Eminönü Pier, Fatih/İstanbul", Latitude = 41.0223m, Longitude = 28.9739m, Rating = 4.6m, Provider = ExternalProvider.Manual, EstimatedCostUsd = 15m, CreatedAt = seedDate },
            // Dubai
            new() { Id = Guid.Parse("44444444-4444-4444-4444-444444444410"), DestinationId = dubaiId, Name = "Burj Khalifa", Category = PoiCategory.Landmark, Address = "1 Sheikh Mohammed bin Rashid Blvd, Dubai", Latitude = 25.1972m, Longitude = 55.2744m, Rating = 4.6m, Provider = ExternalProvider.Manual, EstimatedCostUsd = 40m, CreatedAt = seedDate },
            new() { Id = Guid.Parse("44444444-4444-4444-4444-444444444411"), DestinationId = dubaiId, Name = "Dubai Mall", Category = PoiCategory.Shopping, Address = "Financial Center Rd, Dubai", Latitude = 25.1986m, Longitude = 55.2796m, Rating = 4.5m, Provider = ExternalProvider.Manual, EstimatedCostUsd = 0m, CreatedAt = seedDate },
            new() { Id = Guid.Parse("44444444-4444-4444-4444-444444444412"), DestinationId = dubaiId, Name = "Atlantis Aquaventure", Category = PoiCategory.Attraction, Address = "Palm Jumeirah, Dubai", Latitude = 25.1307m, Longitude = 55.1176m, Rating = 4.4m, Provider = ExternalProvider.Manual, EstimatedCostUsd = 80m, CreatedAt = seedDate },
            // Berlin
            new() { Id = Guid.Parse("44444444-4444-4444-4444-444444444413"), DestinationId = berlinId, Name = "Brandenburg Gate", Category = PoiCategory.Landmark, Address = "Pariser Platz, 10117 Berlin", Latitude = 52.5163m, Longitude = 13.3777m, Rating = 4.6m, Provider = ExternalProvider.Manual, EstimatedCostUsd = 0m, CreatedAt = seedDate },
            new() { Id = Guid.Parse("44444444-4444-4444-4444-444444444414"), DestinationId = berlinId, Name = "Berlin Wall Memorial", Category = PoiCategory.Museum, Address = "Bernauer Str. 111, 13355 Berlin", Latitude = 52.5350m, Longitude = 13.3901m, Rating = 4.5m, Provider = ExternalProvider.Manual, EstimatedCostUsd = 0m, CreatedAt = seedDate },
            new() { Id = Guid.Parse("44444444-4444-4444-4444-444444444415"), DestinationId = berlinId, Name = "Reichstag Building", Category = PoiCategory.Landmark, Address = "Platz der Republik 1, 11011 Berlin", Latitude = 52.5186m, Longitude = 13.3761m, Rating = 4.5m, Provider = ExternalProvider.Manual, EstimatedCostUsd = 0m, CreatedAt = seedDate }
        };

        builder.Entity<PointOfInterest>().HasData(pois);
    }

    private static void SeedVisaAlerts(ModelBuilder builder)
    {
        var alertId = Guid.Parse("55555555-5555-5555-5555-555555555501");

        builder.Entity<VisaAlert>().HasData(
            new VisaAlert
            {
                Id = alertId,
                CountryFrom = "Egypt",
                CountryTo = "France",
                AlertType = VisaAlertType.ProcessingDelay,
                Severity = VisaAlertSeverity.Warning,
                Title = "Possible appointment delays",
                Message = "Visa appointments may take longer during peak travel seasons. Always check the official embassy or visa center website.",
                IsActive = true,
                PublishedAt = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}
