using MediatR;
using Safarni.Application.Abstractions.Authentication;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;

namespace Safarni.Application.Features.VisaApplications.SelectVisaType;

public sealed class SelectVisaTypeCommandHandler : IRequestHandler<SelectVisaTypeCommand, Result<VisaApplicationResponse>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IVisaApplicationRepository _visaApplicationRepository;

    public SelectVisaTypeCommandHandler(
        ICurrentUserService currentUserService,
        IVisaApplicationRepository visaApplicationRepository)
    {
        _currentUserService = currentUserService;
        _visaApplicationRepository = visaApplicationRepository;
    }

    public async Task<Result<VisaApplicationResponse>> Handle(SelectVisaTypeCommand request, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is not { } userId)
        {
            return Result<VisaApplicationResponse>.Failure(AuthenticationErrors.AuthenticationRequired);
        }

        var application = await _visaApplicationRepository.GetByIdForUserAsync(request.Id, userId, cancellationToken);

        if (application is null)
        {
            return Result<VisaApplicationResponse>.Failure(VisaApplicationErrors.NotFound);
        }

        application.VisaType = request.VisaType.Trim();
        application.Status = "VisaTypeSelected";
        application.UpdatedAt = DateTime.UtcNow;

        await _visaApplicationRepository.SaveAsync(cancellationToken);

        return Result<VisaApplicationResponse>.Success(application.ToResponse());
    }
}
