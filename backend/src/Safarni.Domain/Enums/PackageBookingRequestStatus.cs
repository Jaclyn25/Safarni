namespace Safarni.Domain.Enums;

public enum PackageBookingRequestStatus
{
    Pending = 0,
    Accepted = 1,
    Rejected = 2,
    Cancelled = 3,
    OfferSent = 4,
    AwaitingPayment = 5,
    Confirmed = 6,
    InProgress = 7,
    Completed = 8,
    Expired = 9,
    CancelledByTraveler = 10,
    CancelledByAgency = 11,
    Refunded = 12,
    Disputed = 13
}
