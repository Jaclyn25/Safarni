using Safarni.Domain.Enums;

namespace Safarni.Domain.Entities;

public sealed class BookingDispute
{
    public Guid Id { get; set; }
    public Guid PackageBookingRequestId { get; set; }
    public Guid OpenedByUserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public decimal? RequestedRefundAmount { get; set; }
    public BookingDisputeStatus Status { get; set; } = BookingDisputeStatus.Open;
    public string? AdminNote { get; set; }
    public decimal? ResolvedRefundAmount { get; set; }
    public Guid? ResolvedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }

    public PackageBookingRequest PackageBookingRequest { get; set; } = null!;
    public User OpenedByUser { get; set; } = null!;
}
