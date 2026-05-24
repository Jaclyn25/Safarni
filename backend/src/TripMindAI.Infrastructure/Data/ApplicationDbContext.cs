using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TripMindAI.Application.Interfaces;
using TripMindAI.Domain.Entities;
using TripMindAI.Infrastructure.Data.Configurations;

namespace TripMindAI.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<AiToolCall> AiToolCalls => Set<AiToolCall>();
    public DbSet<VisaApplication> VisaApplications => Set<VisaApplication>();
    public DbSet<VisaRule> VisaRules => Set<VisaRule>();
    public DbSet<VisaRuleDocument> VisaRuleDocuments => Set<VisaRuleDocument>();
    public DbSet<VisaApplicationIssue> VisaApplicationIssues => Set<VisaApplicationIssue>();
    public DbSet<VisaAlert> VisaAlerts => Set<VisaAlert>();
    public DbSet<UploadedDocument> UploadedDocuments => Set<UploadedDocument>();
    public DbSet<DocumentAnalysisResult> DocumentAnalysisResults => Set<DocumentAnalysisResult>();
    public DbSet<Destination> Destinations => Set<Destination>();
    public DbSet<PointOfInterest> PointsOfInterest => Set<PointOfInterest>();
    public DbSet<SavedPlace> SavedPlaces => Set<SavedPlace>();
    public DbSet<TripPlan> TripPlans => Set<TripPlan>();
    public DbSet<TripDay> TripDays => Set<TripDay>();
    public DbSet<TripActivity> TripActivities => Set<TripActivity>();
    public DbSet<TripWeatherForecast> TripWeatherForecasts => Set<TripWeatherForecast>();
    public DbSet<FlightSearch> FlightSearches => Set<FlightSearch>();
    public DbSet<FlightOffer> FlightOffers => Set<FlightOffer>();
    public DbSet<HotelSearch> HotelSearches => Set<HotelSearch>();
    public DbSet<HotelOffer> HotelOffers => Set<HotelOffer>();
    public DbSet<CurrencyRate> CurrencyRates => Set<CurrencyRate>();
    public DbSet<TripCostItem> TripCostItems => Set<TripCostItem>();
    public DbSet<GeneratedReport> GeneratedReports => Set<GeneratedReport>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationUserConfiguration).Assembly);
        Seed.SeedData.Seed(modelBuilder);
    }
}
