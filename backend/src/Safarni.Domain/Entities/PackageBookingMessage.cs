namespace Safarni.Domain.Entities;

public sealed class PackageBookingMessage
{
    public Guid Id { get; set; }
    public Guid PackageBookingRequestId { get; set; }
    public Guid SenderUserId { get; set; }
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }

    public PackageBookingRequest PackageBookingRequest { get; set; } = null!;
    public User SenderUser { get; set; } = null!;
}
