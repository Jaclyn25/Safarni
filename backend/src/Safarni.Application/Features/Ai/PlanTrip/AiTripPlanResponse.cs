using System.Collections.Generic;
using Safarni.Application.Features.PublicTrips.Queries;

namespace Safarni.Application.Features.Ai.PlanTrip;

public sealed record AiTripPlanResponse(
    string? GeneratedItinerary,
    IReadOnlyCollection<PublicTripSearchResponse> RelatedPublicTrips);
