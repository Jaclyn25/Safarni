namespace Safarni.Domain.Entities;

public sealed class TravelPreference
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string PreferredDestinationCountry { get; set; } = string.Empty;

    public string TravelPurpose { get; set; } = string.Empty;

    public string TravelStyle { get; set; } = string.Empty;

    public decimal? MinBudget { get; set; }

    public decimal? MaxBudget { get; set; }

    public DateTime UpdatedAt { get; set; }

    public User? User { get; set; }
}
