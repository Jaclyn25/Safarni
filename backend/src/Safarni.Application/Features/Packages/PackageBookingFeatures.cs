using FluentValidation;
using System.Text.Json;
using MediatR;
using Safarni.Application.Abstractions.Notifications;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;
using Safarni.Domain.Entities;
using Safarni.Domain.Enums;

namespace Safarni.Application.Features.Packages;

public sealed record PackageBookingRequestResponse(
    Guid Id,
    Guid PackageId,
    string PackageTitle,
    string Destination,
    DateTime PackageEndDate,
    Guid TravelAgencyId,
    string CompanyName,
    Guid TravelerUserId,
    string TravelerName,
    string TravelerEmail,
    int TravelerCount,
    int AdultCount,
    int ChildCount,
    IReadOnlyList<int> ChildrenAges,
    string ContactPhone,
    string? AlternatePhone,
    string TravelerCountry,
    string TravelerCity,
    string? Message,
    PackageBookingRequestStatus Status,
    string? AgencyResponse,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    DateTime? RespondedAt,
    bool HasReview,
    PackageBookingOfferResponse? CurrentOffer = null,
    BookingPaymentResponse? LatestPayment = null);

public sealed record CreatePackageBookingRequestCommand(
    Guid TravelerUserId,
    Guid PackageId,
    int TravelerCount,
    string? Message,
    int? AdultCount = null,
    IReadOnlyList<int>? ChildrenAges = null,
    string? ContactPhone = null,
    string? AlternatePhone = null,
    string? TravelerCountry = null,
    string? TravelerCity = null)
    : IRequest<Result<PackageBookingRequestResponse>>;

public sealed record ListTravelerBookingRequestsQuery(Guid TravelerUserId)
    : IRequest<Result<IReadOnlyList<PackageBookingRequestResponse>>>;

public sealed record GetTravelerBookingRequestQuery(Guid TravelerUserId, Guid RequestId)
    : IRequest<Result<PackageBookingRequestResponse>>;

public sealed record ListAgencyBookingRequestsQuery(Guid UserId)
    : IRequest<Result<IReadOnlyList<PackageBookingRequestResponse>>>;

public sealed record GetAgencyBookingRequestQuery(Guid UserId, Guid RequestId)
    : IRequest<Result<PackageBookingRequestResponse>>;

public sealed record AcceptPackageBookingRequestCommand(Guid UserId, Guid RequestId, string? Response)
    : IRequest<Result<PackageBookingRequestResponse>>;

public sealed record RejectPackageBookingRequestCommand(Guid UserId, Guid RequestId, string? Response)
    : IRequest<Result<PackageBookingRequestResponse>>;

public sealed class CreatePackageBookingRequestCommandValidator
    : AbstractValidator<CreatePackageBookingRequestCommand>
{
    public CreatePackageBookingRequestCommandValidator()
    {
        RuleFor(command => command.TravelerUserId).NotEmpty();
        RuleFor(command => command.PackageId).NotEmpty();
        RuleFor(command => command.TravelerCount).InclusiveBetween(1, 100);
        RuleFor(command => command.Message).MaximumLength(2000);
        RuleFor(command => command.AdultCount).InclusiveBetween(1, 100)
            .When(command => command.AdultCount is not null);
        RuleForEach(command => command.ChildrenAges).InclusiveBetween(0, 17);
        RuleFor(command => command.ContactPhone).NotEmpty().MaximumLength(40)
            .When(command => command.AdultCount is not null);
        RuleFor(command => command.AlternatePhone).MaximumLength(40);
        RuleFor(command => command.TravelerCountry).NotEmpty().MaximumLength(100)
            .When(command => command.AdultCount is not null);
        RuleFor(command => command.TravelerCity).NotEmpty().MaximumLength(100)
            .When(command => command.AdultCount is not null);
        RuleFor(command => command).Must(command => command.AdultCount is null ||
                command.TravelerCount == command.AdultCount + (command.ChildrenAges?.Count ?? 0))
            .WithMessage("Traveler count must equal adults plus children.");
    }
}

public sealed class ProcessPackageBookingRequestCommandValidator
    : AbstractValidator<AcceptPackageBookingRequestCommand>
{
    public ProcessPackageBookingRequestCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.RequestId).NotEmpty();
        RuleFor(command => command.Response).MaximumLength(2000);
    }
}

public sealed class RejectPackageBookingRequestCommandValidator
    : AbstractValidator<RejectPackageBookingRequestCommand>
{
    public RejectPackageBookingRequestCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.RequestId).NotEmpty();
        RuleFor(command => command.Response)
            .NotEmpty()
            .MaximumLength(2000)
            .WithMessage("A reason is required when rejecting a booking request.");
    }
}

internal static class PackageBookingMapping
{
    public static PackageBookingRequestResponse ToResponse(PackageBookingRequest request) =>
        new(
            request.Id,
            request.TravelPackageId,
            request.TravelPackage.Title,
            request.TravelPackage.Destination,
            request.TravelPackage.EndDate,
            request.TravelPackage.TravelAgencyId,
            request.TravelPackage.TravelAgency.CompanyName,
            request.TravelerUserId,
            request.TravelerUser.FullName,
            request.TravelerUser.Email,
            request.TravelerCount,
            request.AdultCount,
            request.ChildCount,
            DeserializeAges(request.ChildrenAgesJson),
            request.ContactPhone,
            request.AlternatePhone,
            request.TravelerCountry,
            request.TravelerCity,
            request.Message,
            request.Status,
            request.AgencyResponse,
            request.CreatedAt,
            request.UpdatedAt,
            request.RespondedAt,
            request.AgencyReview is not null,
            request.Offers
                .OrderByDescending(offer => offer.Version)
                .Select(PackageBookingOfferMapping.ToResponse)
                .FirstOrDefault(),
            request.Payments
                .OrderByDescending(payment => payment.CreatedAt)
                .Select(payment => BookingPaymentMapping.ToResponse(payment))
                .FirstOrDefault());

    private static IReadOnlyList<int> DeserializeAges(string json)
    {
        try { return JsonSerializer.Deserialize<int[]>(json) ?? []; }
        catch (JsonException) { return []; }
    }
}

public sealed class CreatePackageBookingRequestCommandHandler
    : IRequestHandler<CreatePackageBookingRequestCommand, Result<PackageBookingRequestResponse>>
{
    private readonly ITravelPackageRepository _packages;
    private readonly IPackageBookingRequestRepository _requests;
    private readonly IMarketplaceNotificationService _notifications;

    public CreatePackageBookingRequestCommandHandler(
        ITravelPackageRepository packages,
        IPackageBookingRequestRepository requests,
        IMarketplaceNotificationService notifications)
    {
        _packages = packages;
        _requests = requests;
        _notifications = notifications;
    }

    public async Task<Result<PackageBookingRequestResponse>> Handle(CreatePackageBookingRequestCommand command, CancellationToken cancellationToken)
    {
        var package = await _packages.GetByIdAsync(command.PackageId, cancellationToken: cancellationToken);
        if (package is null || !package.IsPublished || !package.IsActive ||
            package.TravelAgency.VerificationStatus != AgencyVerificationStatus.Verified)
        {
            return Result<PackageBookingRequestResponse>.Failure(PackageBookingErrors.PackageUnavailable);
        }
        if (package.EndDate.Date < DateTime.UtcNow.Date)
        {
            return Result<PackageBookingRequestResponse>.Failure(PackageBookingErrors.PackageExpired);
        }
        if (await _requests.HasOpenRequestAsync(command.PackageId, command.TravelerUserId, cancellationToken))
        {
            return Result<PackageBookingRequestResponse>.Failure(PackageBookingErrors.DuplicateRequest);
        }

        var acceptedSeats = await _requests.GetAcceptedTravelerCountAsync(command.PackageId, cancellationToken);
        if (acceptedSeats + command.TravelerCount > package.AvailableSeats)
        {
            return Result<PackageBookingRequestResponse>.Failure(PackageBookingErrors.PackageFull);
        }

        var request = new PackageBookingRequest
        {
            Id = Guid.NewGuid(),
            TravelPackageId = package.Id,
            TravelPackage = package,
            TravelerUserId = command.TravelerUserId,
            TravelerCount = command.TravelerCount,
            AdultCount = command.AdultCount ?? command.TravelerCount,
            ChildCount = command.ChildrenAges?.Count ?? 0,
            ChildrenAgesJson = JsonSerializer.Serialize(command.ChildrenAges ?? []),
            ContactPhone = ValueOrFallback(command.ContactPhone),
            AlternatePhone = string.IsNullOrWhiteSpace(command.AlternatePhone) ? null : command.AlternatePhone.Trim(),
            TravelerCountry = ValueOrFallback(command.TravelerCountry),
            TravelerCity = ValueOrFallback(command.TravelerCity),
            Message = string.IsNullOrWhiteSpace(command.Message) ? null : command.Message.Trim(),
            Status = PackageBookingRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        await _requests.AddAsync(request, cancellationToken);
        await _requests.SaveChangesAsync(cancellationToken);
        var loaded = await _requests.GetByIdForTravelerAsync(request.Id, command.TravelerUserId, cancellationToken);
        if (loaded is null)
        {
            return Result<PackageBookingRequestResponse>.Failure(PackageBookingErrors.NotFound);
        }
        await _notifications.NotifyUserAsync(
            package.TravelAgency.UserId,
            MarketplaceNotificationType.PackageBookingRequestReceived,
            "New booking request",
            $"{loaded.TravelerUser.FullName} sent a request for {package.Title}.",
            "/agency/booking-requests",
            cancellationToken);
        return Result<PackageBookingRequestResponse>.Success(PackageBookingMapping.ToResponse(loaded!));
    }

    private static string ValueOrFallback(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "Not provided" : value.Trim();
}

public sealed class ListTravelerBookingRequestsQueryHandler
    : IRequestHandler<ListTravelerBookingRequestsQuery, Result<IReadOnlyList<PackageBookingRequestResponse>>>
{
    private readonly IPackageBookingRequestRepository _requests;

    public ListTravelerBookingRequestsQueryHandler(IPackageBookingRequestRepository requests)
    {
        _requests = requests;
    }

    public async Task<Result<IReadOnlyList<PackageBookingRequestResponse>>> Handle(ListTravelerBookingRequestsQuery query, CancellationToken cancellationToken)
    {
        var requests = await _requests.ListByTravelerAsync(query.TravelerUserId, cancellationToken);
        return Result<IReadOnlyList<PackageBookingRequestResponse>>.Success(requests.Select(PackageBookingMapping.ToResponse).ToArray());
    }
}

public sealed class GetTravelerBookingRequestQueryHandler
    : IRequestHandler<GetTravelerBookingRequestQuery, Result<PackageBookingRequestResponse>>
{
    private readonly IPackageBookingRequestRepository _requests;

    public GetTravelerBookingRequestQueryHandler(IPackageBookingRequestRepository requests)
    {
        _requests = requests;
    }

    public async Task<Result<PackageBookingRequestResponse>> Handle(GetTravelerBookingRequestQuery query, CancellationToken cancellationToken)
    {
        var request = await _requests.GetByIdForTravelerAsync(query.RequestId, query.TravelerUserId, cancellationToken);
        return request is null
            ? Result<PackageBookingRequestResponse>.Failure(PackageBookingErrors.NotFound)
            : Result<PackageBookingRequestResponse>.Success(PackageBookingMapping.ToResponse(request));
    }
}

public sealed class ListAgencyBookingRequestsQueryHandler
    : IRequestHandler<ListAgencyBookingRequestsQuery, Result<IReadOnlyList<PackageBookingRequestResponse>>>
{
    private readonly ITravelAgencyRepository _agencies;
    private readonly IPackageBookingRequestRepository _requests;

    public ListAgencyBookingRequestsQueryHandler(ITravelAgencyRepository agencies, IPackageBookingRequestRepository requests)
    {
        _agencies = agencies;
        _requests = requests;
    }

    public async Task<Result<IReadOnlyList<PackageBookingRequestResponse>>> Handle(ListAgencyBookingRequestsQuery query, CancellationToken cancellationToken)
    {
        var agency = await _agencies.GetByUserIdAsync(query.UserId, cancellationToken: cancellationToken);
        if (agency is null) return Result<IReadOnlyList<PackageBookingRequestResponse>>.Failure(TravelAgencyErrors.AgencyAccountRequired);
        var requests = await _requests.ListByAgencyAsync(agency.Id, cancellationToken);
        return Result<IReadOnlyList<PackageBookingRequestResponse>>.Success(requests.Select(PackageBookingMapping.ToResponse).ToArray());
    }
}

public sealed class GetAgencyBookingRequestQueryHandler
    : IRequestHandler<GetAgencyBookingRequestQuery, Result<PackageBookingRequestResponse>>
{
    private readonly ITravelAgencyRepository _agencies;
    private readonly IPackageBookingRequestRepository _requests;

    public GetAgencyBookingRequestQueryHandler(ITravelAgencyRepository agencies, IPackageBookingRequestRepository requests)
    {
        _agencies = agencies;
        _requests = requests;
    }

    public async Task<Result<PackageBookingRequestResponse>> Handle(GetAgencyBookingRequestQuery query, CancellationToken cancellationToken)
    {
        var agency = await _agencies.GetByUserIdAsync(query.UserId, cancellationToken: cancellationToken);
        if (agency is null) return Result<PackageBookingRequestResponse>.Failure(TravelAgencyErrors.AgencyAccountRequired);
        var request = await _requests.GetByIdForAgencyAsync(query.RequestId, agency.Id, cancellationToken);
        return request is null
            ? Result<PackageBookingRequestResponse>.Failure(PackageBookingErrors.NotFound)
            : Result<PackageBookingRequestResponse>.Success(PackageBookingMapping.ToResponse(request));
    }
}

public sealed class AcceptPackageBookingRequestCommandHandler
    : IRequestHandler<AcceptPackageBookingRequestCommand, Result<PackageBookingRequestResponse>>
{
    private readonly ITravelAgencyRepository _agencies;
    private readonly IPackageBookingRequestRepository _requests;
    private readonly IMarketplaceNotificationService _notifications;

    public AcceptPackageBookingRequestCommandHandler(
        ITravelAgencyRepository agencies,
        IPackageBookingRequestRepository requests,
        IMarketplaceNotificationService notifications)
    {
        _agencies = agencies;
        _requests = requests;
        _notifications = notifications;
    }

    public Task<Result<PackageBookingRequestResponse>> Handle(AcceptPackageBookingRequestCommand command, CancellationToken cancellationToken) =>
        Process(command.UserId, command.RequestId, PackageBookingRequestStatus.Accepted, command.Response, cancellationToken);

    internal Task<Result<PackageBookingRequestResponse>> ProcessForRejection(Guid userId, Guid requestId, string response, CancellationToken cancellationToken) =>
        Process(userId, requestId, PackageBookingRequestStatus.Rejected, response, cancellationToken);

    private async Task<Result<PackageBookingRequestResponse>> Process(Guid userId, Guid requestId, PackageBookingRequestStatus status, string? response, CancellationToken cancellationToken)
    {
        var agency = await _agencies.GetByUserIdAsync(userId, cancellationToken: cancellationToken);
        if (agency is null) return Result<PackageBookingRequestResponse>.Failure(TravelAgencyErrors.AgencyAccountRequired);
        var request = await _requests.GetByIdForAgencyAsync(requestId, agency.Id, cancellationToken);
        if (request is null) return Result<PackageBookingRequestResponse>.Failure(PackageBookingErrors.NotFound);
        if (request.Status != PackageBookingRequestStatus.Pending) return Result<PackageBookingRequestResponse>.Failure(PackageBookingErrors.InvalidStatus);
        if (status == PackageBookingRequestStatus.Accepted)
        {
            var acceptedSeats = await _requests.GetAcceptedTravelerCountAsync(request.TravelPackageId, cancellationToken);
            if (acceptedSeats + request.TravelerCount > request.TravelPackage.AvailableSeats)
                return Result<PackageBookingRequestResponse>.Failure(PackageBookingErrors.PackageFull);
        }
        var now = DateTime.UtcNow;
        request.Status = status;
        request.AgencyResponse = string.IsNullOrWhiteSpace(response) ? null : response.Trim();
        request.RespondedAt = now;
        request.UpdatedAt = now;
        await _requests.SaveChangesAsync(cancellationToken);
        await _notifications.NotifyUserAsync(
            request.TravelerUserId,
            status == PackageBookingRequestStatus.Accepted
                ? MarketplaceNotificationType.PackageBookingRequestAccepted
                : MarketplaceNotificationType.PackageBookingRequestRejected,
            status == PackageBookingRequestStatus.Accepted
                ? "Booking request accepted"
                : "Booking request rejected",
            status == PackageBookingRequestStatus.Accepted
                ? $"Your request for {request.TravelPackage.Title} was accepted by {request.TravelPackage.TravelAgency.CompanyName}."
                : $"Your request for {request.TravelPackage.Title} was rejected by {request.TravelPackage.TravelAgency.CompanyName}."
                    + (string.IsNullOrWhiteSpace(request.AgencyResponse) ? string.Empty : $" Note: {request.AgencyResponse}"),
            "/agency-trips",
            cancellationToken);
        return Result<PackageBookingRequestResponse>.Success(PackageBookingMapping.ToResponse(request));
    }
}

public sealed class RejectPackageBookingRequestCommandHandler
    : IRequestHandler<RejectPackageBookingRequestCommand, Result<PackageBookingRequestResponse>>
{
    private readonly AcceptPackageBookingRequestCommandHandler _processor;

    public RejectPackageBookingRequestCommandHandler(AcceptPackageBookingRequestCommandHandler processor)
    {
        _processor = processor;
    }

    public Task<Result<PackageBookingRequestResponse>> Handle(RejectPackageBookingRequestCommand command, CancellationToken cancellationToken) =>
        _processor.ProcessForRejection(command.UserId, command.RequestId, command.Response!, cancellationToken);
}
