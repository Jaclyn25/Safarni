namespace Safarni.Application.Features.Preferences;

public sealed record TravelProfileResponse(
    Guid UserId,
    string PreferredDestinationCountry,
    string TravelPurpose,
    string TravelStyle,
    decimal? MinBudget,
    decimal? MaxBudget,
    string PassportNumber,
    string Nationality,
    DateOnly PassportExpiryDate,
    DateTime UpdatedAt);
