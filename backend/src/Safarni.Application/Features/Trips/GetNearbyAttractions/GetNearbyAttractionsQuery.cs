using System;
using System.Collections.Generic;
using MediatR;
using Safarni.Application.Common.Results;

namespace Safarni.Application.Features.Trips.GetNearbyAttractions;

public sealed record GetNearbyAttractionsQuery(
    Guid TripId,
    int Radius,
    string? Category) : IRequest<Result<List<NearbyAttractionDto>>>;
