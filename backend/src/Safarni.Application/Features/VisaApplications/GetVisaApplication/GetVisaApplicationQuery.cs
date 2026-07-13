using MediatR;
using Safarni.Application.Common.Results;

namespace Safarni.Application.Features.VisaApplications.GetVisaApplication;

public sealed record GetVisaApplicationQuery(Guid Id) : IRequest<Result<VisaApplicationResponse>>;
