using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Safarni.Application.Common.Results;
using Safarni.Application.Features.Trips.GetNearbyAttractions;

namespace Safarni.Application.Abstractions.Travel;

public interface IGeoapifyService
{
    Task<Result<List<NearbyAttractionDto>>> GetNearbyPlacesAsync(
        double latitude,
        double longitude,
        int radiusMeters,
        string? category,
        CancellationToken cancellationToken = default);
}
