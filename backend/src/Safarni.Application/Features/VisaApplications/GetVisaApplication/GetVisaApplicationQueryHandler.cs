using MediatR;
using Safarni.Application.Abstractions.Authentication;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;

namespace Safarni.Application.Features.VisaApplications.GetVisaApplication;

public sealed class GetVisaApplicationQueryHandler : IRequestHandler<GetVisaApplicationQuery, Result<VisaApplicationResponse>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IVisaApplicationRepository _visaApplicationRepository;

    public GetVisaApplicationQueryHandler(
        ICurrentUserService currentUserService,
        IVisaApplicationRepository visaApplicationRepository)
    {
        _currentUserService = currentUserService;
        _visaApplicationRepository = visaApplicationRepository;
    }

    public async Task<Result<VisaApplicationResponse>> Handle(GetVisaApplicationQuery request, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is not { } userId)
        {
            return Result<VisaApplicationResponse>.Failure(AuthenticationErrors.AuthenticationRequired);
        }

        var application = await _visaApplicationRepository.GetByIdForUserAsync(request.Id, userId, cancellationToken);

        return application is null
            ? Result<VisaApplicationResponse>.Failure(VisaApplicationErrors.NotFound)
            : Result<VisaApplicationResponse>.Success(application.ToResponse());
    }
}
