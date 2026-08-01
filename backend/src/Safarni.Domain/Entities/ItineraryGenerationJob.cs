namespace Safarni.Domain.Entities;

public enum ItineraryGenerationJobStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Cancelled
}

public class ItineraryGenerationJob
{
    public Guid Id { get; set; }

    public Guid TripId { get; set; }

    public Guid UserId { get; set; }

    public string Preferences { get; set; } = string.Empty;

    public decimal? BudgetAmount { get; set; }

    public string CurrencyCode { get; set; } = "USD";

    public int TravelerCount { get; set; } = 1;

    public string? IdempotencyKey { get; set; }

    public ItineraryGenerationJobStatus Status { get; set; } =
        ItineraryGenerationJobStatus.Pending;

    public bool IsActive { get; set; } = true;

    public int ProgressPercent { get; set; }

    public string Stage { get; set; } = "Queued";

    public string? ErrorCode { get; set; }

    public string? ErrorMessage { get; set; }

    public Guid? ItineraryId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public string? WorkerId { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public Trip Trip { get; set; } = null!;
}
