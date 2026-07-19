using MediatR;
using Safarni.Application.Abstractions.Authentication;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;
using Safarni.Domain.Entities;

namespace Safarni.Application.Features.VisaApplications.CreateVisaApplication;

public sealed class CreateVisaApplicationCommandHandler : IRequestHandler<CreateVisaApplicationCommand, Result<VisaApplicationResponse>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IVisaApplicationRepository _visaApplicationRepository;

    public CreateVisaApplicationCommandHandler(
        ICurrentUserService currentUserService,
        IVisaApplicationRepository visaApplicationRepository)
    {
        _currentUserService = currentUserService;
        _visaApplicationRepository = visaApplicationRepository;
    }

    public async Task<Result<VisaApplicationResponse>> Handle(CreateVisaApplicationCommand request, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is not { } userId)
        {
            return Result<VisaApplicationResponse>.Failure(AuthenticationErrors.AuthenticationRequired);
        }

        var now = DateTime.UtcNow;
        var application = new VisaApplication
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DestinationCountry = request.DestinationCountry.Trim(),
            TravelPurpose = request.TravelPurpose.Trim(),
            PlannedTravelDate = request.PlannedTravelDate,
            Status = "Draft",
            CreatedAt = now,
            UpdatedAt = now
        };

        await _visaApplicationRepository.AddAsync(application, cancellationToken);

        return Result<VisaApplicationResponse>.Success(application.ToResponse());
    }
}
