using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Safarni.Application.Common.Results;
using Safarni.Application.Features.Trips.GetNearbyAttractions;

namespace Safarni.Application.Abstractions.Travel;

public sealed record DestinationSuggestion(
    string City,
    string Country,
    string CountryCode,
    string Formatted,
    double Latitude,
    double Longitude);

public interface IGeoapifyService
{
    Task<Result<List<DestinationSuggestion>>> SuggestDestinationsAsync(
        string query,
        CancellationToken cancellationToken = default);

    Task<Result<List<NearbyAttractionDto>>> GetNearbyPlacesAsync(
        double latitude,
        double longitude,
        int radiusMeters,
        string? category,
        CancellationToken cancellationToken = default);
}
