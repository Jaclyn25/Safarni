using MediatR;
using Safarni.Application.Common.Results;

namespace Safarni.Application.Features.VisaApplications.ListVisaApplications;

public sealed record ListVisaApplicationsQuery : IRequest<Result<IReadOnlyCollection<VisaApplicationResponse>>>;
