using MediatR;
using Safarni.Application.Common.Results;

namespace Safarni.Application.Features.VisaApplications.SelectVisaType;

public sealed record SelectVisaTypeCommand(Guid Id, string VisaType) : IRequest<Result<VisaApplicationResponse>>;
