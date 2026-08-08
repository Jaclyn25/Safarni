using FluentValidation;
using MediatR;
using Safarni.Application.Abstractions.Notifications;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;
using Safarni.Application.Features.Agencies.Common;
using Safarni.Domain.Entities;
using Safarni.Domain.Enums;

namespace Safarni.Application.Features.Agencies.Onboarding;

public sealed record AgencyOnboardingStatusResponse(
    bool IsEmailVerified,
    bool IsCompanyProfileComplete,
    Guid? AgencyId);

public sealed record GetAgencyOnboardingStatusQuery(Guid UserId)
    : IRequest<Result<AgencyOnboardingStatusResponse>>;

public sealed class GetAgencyOnboardingStatusQueryHandler
    : IRequestHandler<GetAgencyOnboardingStatusQuery, Result<AgencyOnboardingStatusResponse>>
{
    private readonly IUserRepository _users;
    private readonly ITravelAgencyRepository _agencies;

    public GetAgencyOnboardingStatusQueryHandler(
        IUserRepository users,
        ITravelAgencyRepository agencies)
    {
        _users = users;
        _agencies = agencies;
    }

    public async Task<Result<AgencyOnboardingStatusResponse>> Handle(
        GetAgencyOnboardingStatusQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(request.UserId, cancellationToken);
        var agency = await _agencies.GetByUserIdAsync(
            request.UserId,
            cancellationToken: cancellationToken);

        return Result<AgencyOnboardingStatusResponse>.Success(
            new AgencyOnboardingStatusResponse(
                user?.IsEmailVerified ?? false,
                agency is not null,
                agency?.Id));
    }
}

public sealed record CompleteAgencyOnboardingCommand(
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

public sealed class CompleteAgencyOnboardingCommandValidator
    : AbstractValidator<CompleteAgencyOnboardingCommand>
{
    public CompleteAgencyOnboardingCommandValidator()
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

    private static bool BeOptionalHttpUrl(string? value) =>
        string.IsNullOrWhiteSpace(value) ||
        Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}

public sealed class CompleteAgencyOnboardingCommandHandler
    : IRequestHandler<CompleteAgencyOnboardingCommand, Result<AgencyProfileResponse>>
{
    private readonly IUserRepository _users;
    private readonly ITravelAgencyRepository _agencies;
    private readonly IMarketplaceNotificationService _notifications;

    public CompleteAgencyOnboardingCommandHandler(
        IUserRepository users,
        ITravelAgencyRepository agencies,
        IMarketplaceNotificationService notifications)
    {
        _users = users;
        _agencies = agencies;
        _notifications = notifications;
    }

    public async Task<Result<AgencyProfileResponse>> Handle(
        CompleteAgencyOnboardingCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return Result<AgencyProfileResponse>.Failure(TravelAgencyErrors.NotFound);
        }

        if (!user.IsEmailVerified)
        {
            return Result<AgencyProfileResponse>.Failure(
                TravelAgencyErrors.EmailVerificationRequired);
        }

        if (await _agencies.GetByUserIdAsync(
                request.UserId,
                cancellationToken: cancellationToken) is not null)
        {
            return Result<AgencyProfileResponse>.Failure(
                TravelAgencyErrors.OnboardingAlreadyCompleted);
        }

        var companyName = NormalizeCompanyName(request.CompanyName);
        if (await _agencies.CompanyNameExistsAsync(
                companyName,
                Guid.Empty,
                cancellationToken))
        {
            return Result<AgencyProfileResponse>.Failure(
                TravelAgencyErrors.CompanyNameAlreadyExists);
        }

        var agency = new TravelAgency
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            CompanyName = companyName,
            Description = request.Description.Trim(),
            Address = request.Address.Trim(),
            City = request.City.Trim(),
            Country = request.Country.Trim(),
            CompanyPhone = request.CompanyPhone.Trim(),
            LogoUrl = TrimToNull(request.LogoUrl),
            WebsiteUrl = TrimToNull(request.WebsiteUrl),
            VerificationStatus = AgencyVerificationStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        if (!await _agencies.TryAddAsync(agency, cancellationToken))
        {
            return Result<AgencyProfileResponse>.Failure(
                TravelAgencyErrors.CompanyNameAlreadyExists);
        }

        await _notifications.NotifyAgencyReviewersAsync(
            MarketplaceNotificationType.AgencyProfileSubmitted,
            "New agency profile submitted",
            $"{agency.CompanyName} completed its company profile and can begin verification.",
            $"/admin/agencies/{agency.Id}",
            cancellationToken);

        return Result<AgencyProfileResponse>.Success(
            AgencyResponseMapper.ToProfile(agency));
    }

    private static string NormalizeCompanyName(string companyName) =>
        string.Join(
            ' ',
            companyName.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static string? TrimToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
