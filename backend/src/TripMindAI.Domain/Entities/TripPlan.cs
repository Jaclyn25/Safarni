using TripMindAI.Domain.Enums;

namespace TripMindAI.Domain.Entities;

public class TripPlan
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public Guid? ConversationId { get; set; }
    public Conversation? Conversation { get; set; }
    public Guid? VisaApplicationId { get; set; }
    public VisaApplication? VisaApplication { get; set; }
    public string Title { get; set; } = string.Empty;
    public string DestinationCountry { get; set; } = string.Empty;
    public string? DestinationCity { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal? BudgetUsd { get; set; }
    public decimal? BudgetEgp { get; set; }
    public TripStatus Status { get; set; }
    public string? AiSummary { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<TripDay> Days { get; set; } = new List<TripDay>();
    public ICollection<TripCostItem> CostItems { get; set; } = new List<TripCostItem>();
    public ICollection<FlightSearch> FlightSearches { get; set; } = new List<FlightSearch>();
    public ICollection<HotelSearch> HotelSearches { get; set; } = new List<HotelSearch>();
    public ICollection<GeneratedReport> GeneratedReports { get; set; } = new List<GeneratedReport>();
}
