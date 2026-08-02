using System;
using MediatR;
using Safarni.Application.Common.Results;

namespace Safarni.Application.Features.Trips.GetTripRoute;

public sealed record GetTripRouteQuery(Guid TripId) : IRequest<Result<TripRouteResponse>>;
