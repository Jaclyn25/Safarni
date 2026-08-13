using MediatR;
using Safarni.Application.Common.Results;
using System;

namespace Safarni.Application.Features.PublicTrips.Commands
{
    public sealed record ReportPublicTripCommand(
        Guid VersionId,
        string? Reason
    ) : IRequest<Result<ReportPublicTripResult>>;

    public sealed record ReportPublicTripResult(
        Guid VersionId,
        bool Success
    );
}