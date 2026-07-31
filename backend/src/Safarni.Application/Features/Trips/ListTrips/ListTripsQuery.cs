using MediatR;
using Safarni.Application.Common.Results;

namespace Safarni.Application.Features.Trips.ListTrips;

public sealed record ListTripsQuery : IRequest<Result<IReadOnlyCollection<TripResponse>>>;
