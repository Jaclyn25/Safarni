using MediatR;
using Safarni.Application.Common.Results;

namespace Safarni.Application.Features.VisaApplications.CreateVisaApplication;

public sealed record CreateVisaApplicationCommand(
    string DestinationCountry,
    string TravelPurpose,
    DateOnly PlannedTravelDate) : IRequest<Result<VisaApplicationResponse>>;
