using FluentValidation;
using MediatR;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;
using Safarni.Domain.Entities;
using Safarni.Domain.Enums;

namespace Safarni.Application.Features.Agencies.Payout;

public sealed record AgencyPayoutProfileResponse(
    Guid Id,
    AgencyPayoutMethod Method,
    AgencyPayoutProfileStatus Status,
    string AccountHolderName,
    string Country,
    string? BankName,
    string? Iban,
    string? AccountNumber,
    string? SwiftCode,
    string? WalletProvider,
    string? WalletNumber,
    DateTime? VerifiedAt);

public sealed record GetMyAgencyPayoutProfileQuery(Guid UserId)
    : IRequest<Result<AgencyPayoutProfileResponse>>;

public sealed record UpsertMyAgencyPayoutProfileCommand(
    Guid UserId,
    AgencyPayoutMethod Method,
    string AccountHolderName,
    string Country,
    string? BankName,
    string? Iban,
    string? AccountNumber,
    string? SwiftCode,
    string? WalletProvider,
    string? WalletNumber)
    : IRequest<Result<AgencyPayoutProfileResponse>>;

public sealed record AdminAgencyPayoutProfileResponse(
    Guid AgencyId,
    string CompanyName,
    string AgencyEmail,
    AgencyPayoutProfileResponse Profile);

public sealed record ListAgencyPayoutProfilesQuery(Guid AdminUserId, AgencyPayoutProfileStatus? Status)
    : IRequest<Result<IReadOnlyList<AdminAgencyPayoutProfileResponse>>>;

public sealed record ReviewAgencyPayoutProfileCommand(Guid AdminUserId, Guid AgencyId, bool Approve, string? ReviewNote)
    : IRequest<Result<AdminAgencyPayoutProfileResponse>>;

public sealed class UpsertMyAgencyPayoutProfileCommandValidator
    : AbstractValidator<UpsertMyAgencyPayoutProfileCommand>
{
    public UpsertMyAgencyPayoutProfileCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.AccountHolderName).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Country).NotEmpty().MaximumLength(100);
        RuleFor(command => command.BankName).MaximumLength(200);
        RuleFor(command => command.Iban).MaximumLength(100);
        RuleFor(command => command.AccountNumber).MaximumLength(100);
        RuleFor(command => command.SwiftCode).MaximumLength(20);
        RuleFor(command => command.WalletProvider).MaximumLength(100);
        RuleFor(command => command.WalletNumber).MaximumLength(100);
        RuleFor(command => command).Must(command => command.Method == AgencyPayoutMethod.BankTransfer
                ? !string.IsNullOrWhiteSpace(command.BankName) &&
                  (!string.IsNullOrWhiteSpace(command.Iban) || !string.IsNullOrWhiteSpace(command.AccountNumber))
                : !string.IsNullOrWhiteSpace(command.WalletProvider) && !string.IsNullOrWhiteSpace(command.WalletNumber))
            .WithMessage("Complete the payout fields required for the selected payment method.");
    }
}

internal static class AgencyPayoutMapping
{
    public static AgencyPayoutProfileResponse ToResponse(AgencyPayoutProfile profile) => new(
        profile.Id, profile.Method, profile.Status, profile.AccountHolderName, profile.Country,
        profile.BankName, profile.Iban, profile.AccountNumber, profile.SwiftCode,
        profile.WalletProvider, profile.WalletNumber, profile.VerifiedAt);
}

public sealed class GetMyAgencyPayoutProfileQueryHandler
    : IRequestHandler<GetMyAgencyPayoutProfileQuery, Result<AgencyPayoutProfileResponse>>
{
    private readonly ITravelAgencyRepository _agencies;

    public GetMyAgencyPayoutProfileQueryHandler(ITravelAgencyRepository agencies) => _agencies = agencies;

    public async Task<Result<AgencyPayoutProfileResponse>> Handle(GetMyAgencyPayoutProfileQuery query, CancellationToken cancellationToken)
    {
        var profile = await _agencies.GetPayoutProfileByUserIdAsync(query.UserId, cancellationToken);
        return profile is null
            ? Result<AgencyPayoutProfileResponse>.Failure(TravelAgencyErrors.NotFound)
            : Result<AgencyPayoutProfileResponse>.Success(AgencyPayoutMapping.ToResponse(profile));
    }
}

public sealed class UpsertMyAgencyPayoutProfileCommandHandler
    : IRequestHandler<UpsertMyAgencyPayoutProfileCommand, Result<AgencyPayoutProfileResponse>>
{
    private readonly ITravelAgencyRepository _agencies;

    public UpsertMyAgencyPayoutProfileCommandHandler(ITravelAgencyRepository agencies) => _agencies = agencies;

    public async Task<Result<AgencyPayoutProfileResponse>> Handle(UpsertMyAgencyPayoutProfileCommand command, CancellationToken cancellationToken)
    {
        var agency = await _agencies.GetByUserIdAsync(command.UserId, cancellationToken: cancellationToken);
        if (agency is null) return Result<AgencyPayoutProfileResponse>.Failure(TravelAgencyErrors.NotFound);
        var profile = await _agencies.GetPayoutProfileByUserIdAsync(command.UserId, cancellationToken);
        var now = DateTime.UtcNow;
        var isNew = profile is null;
        profile ??= new AgencyPayoutProfile { Id = Guid.NewGuid(), TravelAgencyId = agency.Id, TravelAgency = agency, CreatedAt = now };
        profile.Method = command.Method;
        profile.Status = AgencyPayoutProfileStatus.PendingReview;
        profile.VerifiedAt = null;
        profile.AccountHolderName = command.AccountHolderName.Trim();
        profile.Country = command.Country.Trim();
        profile.BankName = TrimOrNull(command.BankName);
        profile.Iban = TrimOrNull(command.Iban);
        profile.AccountNumber = TrimOrNull(command.AccountNumber);
        profile.SwiftCode = TrimOrNull(command.SwiftCode);
        profile.WalletProvider = TrimOrNull(command.WalletProvider);
        profile.WalletNumber = TrimOrNull(command.WalletNumber);
        profile.UpdatedAt = now;
        if (isNew)
            await _agencies.AddPayoutProfileAsync(profile, cancellationToken);
        await _agencies.SaveChangesAsync(cancellationToken);
        return Result<AgencyPayoutProfileResponse>.Success(AgencyPayoutMapping.ToResponse(profile));
    }

    private static string? TrimOrNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class ListAgencyPayoutProfilesQueryHandler
    : IRequestHandler<ListAgencyPayoutProfilesQuery, Result<IReadOnlyList<AdminAgencyPayoutProfileResponse>>>
{
    private readonly ITravelAgencyRepository _agencies;
    public ListAgencyPayoutProfilesQueryHandler(ITravelAgencyRepository agencies) => _agencies = agencies;
    public async Task<Result<IReadOnlyList<AdminAgencyPayoutProfileResponse>>> Handle(ListAgencyPayoutProfilesQuery query, CancellationToken cancellationToken)
    {
        var agencies = await _agencies.ListPayoutProfilesAsync(query.Status, cancellationToken);
        return Result<IReadOnlyList<AdminAgencyPayoutProfileResponse>>.Success(agencies.Select(agency => new AdminAgencyPayoutProfileResponse(
            agency.Id, agency.CompanyName, agency.User.Email, AgencyPayoutMapping.ToResponse(agency.PayoutProfile!))).ToList());
    }
}

public sealed class ReviewAgencyPayoutProfileCommandHandler
    : IRequestHandler<ReviewAgencyPayoutProfileCommand, Result<AdminAgencyPayoutProfileResponse>>
{
    private readonly ITravelAgencyRepository _agencies;
    public ReviewAgencyPayoutProfileCommandHandler(ITravelAgencyRepository agencies) => _agencies = agencies;
    public async Task<Result<AdminAgencyPayoutProfileResponse>> Handle(ReviewAgencyPayoutProfileCommand command, CancellationToken cancellationToken)
    {
        var agency = await _agencies.GetByIdAsync(command.AgencyId, cancellationToken: cancellationToken);
        if (agency?.PayoutProfile is null)
            return Result<AdminAgencyPayoutProfileResponse>.Failure(TravelAgencyErrors.NotFound);
        agency.PayoutProfile.Status = command.Approve ? AgencyPayoutProfileStatus.Verified : AgencyPayoutProfileStatus.Rejected;
        agency.PayoutProfile.VerifiedAt = command.Approve ? DateTime.UtcNow : null;
        await _agencies.SaveChangesAsync(cancellationToken);
        return Result<AdminAgencyPayoutProfileResponse>.Success(new AdminAgencyPayoutProfileResponse(
            agency.Id, agency.CompanyName, agency.User.Email, AgencyPayoutMapping.ToResponse(agency.PayoutProfile)));
    }
}
