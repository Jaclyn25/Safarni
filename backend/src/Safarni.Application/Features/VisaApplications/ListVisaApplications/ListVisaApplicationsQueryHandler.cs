using MediatR;
using Safarni.Application.Abstractions.Authentication;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;

namespace Safarni.Application.Features.VisaApplications.ListVisaApplications;

public sealed class ListVisaApplicationsQueryHandler : IRequestHandler<ListVisaApplicationsQuery, Result<IReadOnlyCollection<VisaApplicationResponse>>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IVisaApplicationRepository _visaApplicationRepository;

    public ListVisaApplicationsQueryHandler(
        ICurrentUserService currentUserService,
        IVisaApplicationRepository visaApplicationRepository)
    {
        _currentUserService = currentUserService;
        _visaApplicationRepository = visaApplicationRepository;
    }

    public async Task<Result<IReadOnlyCollection<VisaApplicationResponse>>> Handle(ListVisaApplicationsQuery request, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is not { } userId)
        {
            return Result<IReadOnlyCollection<VisaApplicationResponse>>.Failure(AuthenticationErrors.AuthenticationRequired);
        }

        var applications = await _visaApplicationRepository.ListByUserIdAsync(userId, cancellationToken);

        return Result<IReadOnlyCollection<VisaApplicationResponse>>.Success(
            applications.Select(application => application.ToResponse()).ToArray());
    }
}
