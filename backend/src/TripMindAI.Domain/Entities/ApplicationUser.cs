using Microsoft.AspNetCore.Identity;
using TripMindAI.Domain.Enums;

namespace TripMindAI.Domain.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
    public string NationalityCountry { get; set; } = "Egypt";
    public string PreferredLanguage { get; set; } = "ar";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();
    public ICollection<TripPlan> TripPlans { get; set; } = new List<TripPlan>();
    public ICollection<VisaApplication> VisaApplications { get; set; } = new List<VisaApplication>();
    public ICollection<UploadedDocument> UploadedDocuments { get; set; } = new List<UploadedDocument>();
    public ICollection<FlightSearch> FlightSearches { get; set; } = new List<FlightSearch>();
    public ICollection<HotelSearch> HotelSearches { get; set; } = new List<HotelSearch>();
    public ICollection<SavedPlace> SavedPlaces { get; set; } = new List<SavedPlace>();
    public ICollection<GeneratedReport> GeneratedReports { get; set; } = new List<GeneratedReport>();
}
