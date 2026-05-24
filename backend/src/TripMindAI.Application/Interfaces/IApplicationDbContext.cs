using Microsoft.EntityFrameworkCore;
using TripMindAI.Domain.Entities;

namespace TripMindAI.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Conversation> Conversations { get; }
    DbSet<Message> Messages { get; }
    DbSet<AiToolCall> AiToolCalls { get; }
    DbSet<VisaApplication> VisaApplications { get; }
    DbSet<VisaRule> VisaRules { get; }
    DbSet<VisaRuleDocument> VisaRuleDocuments { get; }
    DbSet<VisaApplicationIssue> VisaApplicationIssues { get; }
    DbSet<VisaAlert> VisaAlerts { get; }
    DbSet<UploadedDocument> UploadedDocuments { get; }
    DbSet<DocumentAnalysisResult> DocumentAnalysisResults { get; }
    DbSet<Destination> Destinations { get; }
    DbSet<PointOfInterest> PointsOfInterest { get; }
    DbSet<SavedPlace> SavedPlaces { get; }
    DbSet<TripPlan> TripPlans { get; }
    DbSet<TripDay> TripDays { get; }
    DbSet<TripActivity> TripActivities { get; }
    DbSet<TripWeatherForecast> TripWeatherForecasts { get; }
    DbSet<FlightSearch> FlightSearches { get; }
    DbSet<FlightOffer> FlightOffers { get; }
    DbSet<HotelSearch> HotelSearches { get; }
    DbSet<HotelOffer> HotelOffers { get; }
    DbSet<CurrencyRate> CurrencyRates { get; }
    DbSet<TripCostItem> TripCostItems { get; }
    DbSet<GeneratedReport> GeneratedReports { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
