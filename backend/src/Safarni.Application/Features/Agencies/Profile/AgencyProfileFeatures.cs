using FluentValidation;
using MediatR;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Abstractions.Notifications;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;
using Safarni.Application.Features.Agencies.Common;
using Safarni.Domain.Enums;

namespace Safarni.Application.Features.Agencies.Profile;

public sealed record GetMyAgencyProfileQuery(Guid UserId)
    : IRequest<Result<AgencyProfileResponse>>;

public sealed class GetMyAgencyProfileQueryHandler
    : IRequestHandler<GetMyAgencyProfileQuery, Result<AgencyProfileResponse>>
{
    private readonly ITravelAgencyRepository _agencies;

    public GetMyAgencyProfileQueryHandler(ITravelAgencyRepository agencies)
    {
        _agencies = agencies;
    }

    public async Task<Result<AgencyProfileResponse>> Handle(
        GetMyAgencyProfileQuery request,
        CancellationToken cancellationToken)
    {
        var agency = await _agencies.GetByUserIdAsync(
            request.UserId,
            cancellationToken: cancellationToken);

        return agency is null
            ? Result<AgencyProfileResponse>.Failure(TravelAgencyErrors.NotFound)
            : Result<AgencyProfileResponse>.Success(AgencyResponseMapper.ToProfile(agency));
    }
}

public sealed record UpdateMyAgencyProfileCommand(
    Guid UserId,
    string CompanyName,
    string Description,
    string Address,
    string City,
    string Country,
    string CompanyPhone,
    string? LogoUrl,
    string? WebsiteUrl)
    : IRequest<Result<AgencyProfileResponse>>;

public sealed class UpdateMyAgencyProfileCommandValidator
    : AbstractValidator<UpdateMyAgencyProfileCommand>
{
    public UpdateMyAgencyProfileCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.CompanyName).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Description).NotEmpty().MaximumLength(2000);
        RuleFor(command => command.Address).NotEmpty().MaximumLength(500);
        RuleFor(command => command.City).NotEmpty().MaximumLength(100);
        RuleFor(command => command.Country).NotEmpty().MaximumLength(100);
        RuleFor(command => command.CompanyPhone).NotEmpty().MaximumLength(30);
        RuleFor(command => command.LogoUrl)
            .MaximumLength(500)
            .Must(BeOptionalHttpUrl)
            .WithMessage("Logo URL must use HTTP or HTTPS.");
        RuleFor(command => command.WebsiteUrl)
            .MaximumLength(500)
            .Must(BeOptionalHttpUrl)
            .WithMessage("Website URL must use HTTP or HTTPS.");
    }

    private static bool BeOptionalHttpUrl(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ||
            Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}

public sealed class UpdateMyAgencyProfileCommandHandler
    : IRequestHandler<UpdateMyAgencyProfileCommand, Result<AgencyProfileResponse>>
{
    private readonly ITravelAgencyRepository _agencies;
    private readonly IMarketplaceNotificationService _notifications;

    public UpdateMyAgencyProfileCommandHandler(
        ITravelAgencyRepository agencies,
        IMarketplaceNotificationService notifications)
    {
        _agencies = agencies;
        _notifications = notifications;
    }

    public async Task<Result<AgencyProfileResponse>> Handle(
        UpdateMyAgencyProfileCommand request,
        CancellationToken cancellationToken)
    {
        var agency = await _agencies.GetByUserIdAsync(
            request.UserId,
            cancellationToken: cancellationToken);
        if (agency is null)
        {
            return Result<AgencyProfileResponse>.Failure(TravelAgencyErrors.NotFound);
        }

        var normalizedCompanyName = NormalizeCompanyName(request.CompanyName);
        if (await _agencies.CompanyNameExistsAsync(
                normalizedCompanyName,
                agency.Id,
                cancellationToken))
        {
            return Result<AgencyProfileResponse>.Failure(
                TravelAgencyErrors.CompanyNameAlreadyExists);
        }

        var verificationSensitiveDetailsChanged =
            !string.Equals(agency.CompanyName, normalizedCompanyName, StringComparison.Ordinal) ||
            !string.Equals(agency.Address, request.Address.Trim(), StringComparison.Ordinal) ||
            !string.Equals(agency.City, request.City.Trim(), StringComparison.Ordinal) ||
            !string.Equals(agency.Country, request.Country.Trim(), StringComparison.Ordinal);

        agency.CompanyName = normalizedCompanyName;
        agency.Description = request.Description.Trim();
        agency.Address = request.Address.Trim();
        agency.City = request.City.Trim();
        agency.Country = request.Country.Trim();
        agency.CompanyPhone = request.CompanyPhone.Trim();
        agency.LogoUrl = TrimToNull(request.LogoUrl);
        agency.WebsiteUrl = TrimToNull(request.WebsiteUrl);
        agency.UpdatedAt = DateTime.UtcNow;

        if (verificationSensitiveDetailsChanged &&
            agency.VerificationStatus == AgencyVerificationStatus.Verified)
        {
            agency.VerificationStatus = AgencyVerificationStatus.ChangesRequested;
            agency.VerificationDecisionReason =
                "Verification-sensitive company information changed and requires review.";
            agency.VerificationReviewedAt = null;
            agency.VerificationReviewedByUserId = null;
        }

        await _agencies.SaveChangesAsync(cancellationToken);
        await _notifications.NotifyAgencyReviewersAsync(
            MarketplaceNotificationType.AgencyProfileUpdated,
            "Agency profile updated",
            $"{agency.CompanyName} updated its company information.",
            $"/admin/agencies/{agency.Id}",
            cancellationToken);
        return Result<AgencyProfileResponse>.Success(AgencyResponseMapper.ToProfile(agency));
    }

    private static string NormalizeCompanyName(string companyName)
    {
        return string.Join(
            ' ',
            companyName.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    private static string? TrimToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
