namespace Safarni.Domain.Entities;

public sealed class VisaApplication
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string DestinationCountry { get; set; } = string.Empty;

    public string TravelPurpose { get; set; } = string.Empty;

    public DateOnly PlannedTravelDate { get; set; }

    public string? VisaType { get; set; }

    public string Status { get; set; } = "Draft";

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public User? User { get; set; }
}
