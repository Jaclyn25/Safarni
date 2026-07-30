
using MediatR;
using Safarni.Application.Common.Results;


namespace Safarni.Application.Features.Trips.DeleteTrip;

public sealed record DeleteTripCommand(Guid Id) : IRequest<Result>;