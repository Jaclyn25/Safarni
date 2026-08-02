using System.Collections.Generic;

namespace Safarni.Application.Features.Trips.GetTripRoute;

public class TripRouteResponse
{
    public List<TripRouteActivityDto> Activities { get; set; } = new();
}
