namespace Safarni.Application.Features.VisaApplications;

public sealed record VisaApplicationResponse(
    Guid Id,
    Guid UserId,
    string DestinationCountry,
    string TravelPurpose,
    DateOnly PlannedTravelDate,
    string? VisaType,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt);
