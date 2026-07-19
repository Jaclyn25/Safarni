namespace Safarni.Domain.Entities;

public sealed class PassportInfo
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string PassportNumber { get; set; } = string.Empty;

    public string Nationality { get; set; } = string.Empty;

    public DateOnly ExpiryDate { get; set; }

    public DateTime UpdatedAt { get; set; }

    public User? User { get; set; }
}
