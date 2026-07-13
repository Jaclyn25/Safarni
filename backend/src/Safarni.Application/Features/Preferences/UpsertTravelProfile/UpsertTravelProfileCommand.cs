using MediatR;
using Safarni.Application.Common.Results;

namespace Safarni.Application.Features.Preferences.UpsertTravelProfile;

public sealed record UpsertTravelProfileCommand(
    string PreferredDestinationCountry,
    string TravelPurpose,
    string TravelStyle,
    decimal? MinBudget,
    decimal? MaxBudget,
    string PassportNumber,
    string Nationality,
    DateOnly PassportExpiryDate) : IRequest<Result<TravelProfileResponse>>;
