using FluentValidation;
using MediatR;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;
using Safarni.Application.Features.Packages;
using Safarni.Domain.Entities;
using Safarni.Domain.Enums;

namespace Safarni.Application.Features.Agencies.Public;

public sealed record PublicAgencyReviewResponse(
    Guid Id,
    string TravelerName,
    string PackageTitle,
    int Rating,
    string Comment,
    string? AgencyResponse,
    DateTime CreatedAt,
    DateTime? AgencyRespondedAt);

public sealed record PublicAgencyProfileResponse(
    Guid AgencyId,
    string CompanyName,
    string Description,
    string City,
    string Country,
    string CompanyPhone,
    string? LogoUrl,
    string? WebsiteUrl,
    bool IsVerified,
    DateTime MemberSince,
    double AverageRating,
    int ReviewCount,
    int VerifiedBookings,
    int TravelersServed,
    IReadOnlyList<TravelPackageResponse> Packages,
    IReadOnlyList<PublicAgencyReviewResponse> RecentReviews);

public sealed record GetPublicAgencyQuery(Guid AgencyId) : IRequest<Result<PublicAgencyProfileResponse>>;
public sealed record ListPublicAgencyReviewsQuery(Guid AgencyId, int Page, int PageSize)
    : IRequest<Result<PagedResult<PublicAgencyReviewResponse>>>;
public sealed record CreateAgencyReviewCommand(Guid TravelerUserId, Guid AgencyId, Guid BookingRequestId, int Rating, string Comment)
    : IRequest<Result<PublicAgencyReviewResponse>>;
public sealed record RespondToAgencyReviewCommand(Guid AgencyUserId, Guid ReviewId, string Response)
    : IRequest<Result<PublicAgencyReviewResponse>>;

public sealed class CreateAgencyReviewCommandValidator : AbstractValidator<CreateAgencyReviewCommand>
{
    public CreateAgencyReviewCommandValidator()
    {
        RuleFor(command => command.Rating).InclusiveBetween(1, 5);
        RuleFor(command => command.Comment).NotEmpty().MaximumLength(3000);
    }
}

public sealed class RespondToAgencyReviewCommandValidator : AbstractValidator<RespondToAgencyReviewCommand>
{
    public RespondToAgencyReviewCommandValidator() =>
        RuleFor(command => command.Response).NotEmpty().MaximumLength(2000);
}

internal static class PublicAgencyReviewMapping
{
    public static PublicAgencyReviewResponse ToResponse(AgencyReview review) => new(
        review.Id,
        FirstName(review.TravelerUser.FullName),
        review.PackageBookingRequest.TravelPackage.Title,
        review.Rating,
        review.Comment,
        review.AgencyResponse,
        review.CreatedAt,
        review.AgencyRespondedAt);

    private static string FirstName(string fullName) =>
        string.IsNullOrWhiteSpace(fullName) ? "Safarni traveler" : fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
}

public sealed class GetPublicAgencyQueryHandler : IRequestHandler<GetPublicAgencyQuery, Result<PublicAgencyProfileResponse>>
{
    private readonly ITravelAgencyRepository _agencies;
    private readonly ITravelPackageRepository _packages;
    private readonly IAgencyReviewRepository _reviews;
    private readonly IPackageBookingRequestRepository _bookings;

    public GetPublicAgencyQueryHandler(ITravelAgencyRepository agencies, ITravelPackageRepository packages,
        IAgencyReviewRepository reviews, IPackageBookingRequestRepository bookings)
    {
        _agencies = agencies;
        _packages = packages;
        _reviews = reviews;
        _bookings = bookings;
    }

    public async Task<Result<PublicAgencyProfileResponse>> Handle(GetPublicAgencyQuery request, CancellationToken cancellationToken)
    {
        var agency = await _agencies.GetByIdAsync(request.AgencyId, cancellationToken: cancellationToken);
        if (agency is null || agency.VerificationStatus != AgencyVerificationStatus.Verified)
            return Result<PublicAgencyProfileResponse>.Failure(TravelAgencyErrors.NotFound);
        var packages = await _packages.ListPublishedByAgencyAsync(agency.Id, cancellationToken);
        var summary = await _reviews.GetSummaryAsync(agency.Id, cancellationToken);
        var recent = await _reviews.ListPublishedAsync(agency.Id, 1, 6, cancellationToken);
        var verifiedBookings = await _bookings.CountAcceptedByAgencyAsync(agency.Id, cancellationToken);
        var travelersServed = await _bookings.SumAcceptedTravelersByAgencyAsync(agency.Id, cancellationToken);
        return Result<PublicAgencyProfileResponse>.Success(new PublicAgencyProfileResponse(
            agency.Id, agency.CompanyName, agency.Description, agency.City, agency.Country,
            agency.CompanyPhone, agency.LogoUrl, agency.WebsiteUrl, true, agency.CreatedAt,
            summary.AverageRating, summary.ReviewCount, verifiedBookings, travelersServed,
            packages.Select(TravelPackageMapping.ToResponse).ToArray(),
            recent.Select(PublicAgencyReviewMapping.ToResponse).ToArray()));
    }
}

public sealed class ListPublicAgencyReviewsQueryHandler
    : IRequestHandler<ListPublicAgencyReviewsQuery, Result<PagedResult<PublicAgencyReviewResponse>>>
{
    private readonly ITravelAgencyRepository _agencies;
    private readonly IAgencyReviewRepository _reviews;
    public ListPublicAgencyReviewsQueryHandler(ITravelAgencyRepository agencies, IAgencyReviewRepository reviews)
    {
        _agencies = agencies;
        _reviews = reviews;
    }

    public async Task<Result<PagedResult<PublicAgencyReviewResponse>>> Handle(ListPublicAgencyReviewsQuery request, CancellationToken cancellationToken)
    {
        var agency = await _agencies.GetByIdAsync(request.AgencyId, cancellationToken: cancellationToken);
        if (agency is null || agency.VerificationStatus != AgencyVerificationStatus.Verified)
            return Result<PagedResult<PublicAgencyReviewResponse>>.Failure(TravelAgencyErrors.NotFound);
        var page = Math.Max(1, request.Page);
        var size = Math.Clamp(request.PageSize, 1, 50);
        var items = await _reviews.ListPublishedAsync(request.AgencyId, page, size, cancellationToken);
        var count = await _reviews.CountPublishedAsync(request.AgencyId, cancellationToken);
        return Result<PagedResult<PublicAgencyReviewResponse>>.Success(new PagedResult<PublicAgencyReviewResponse>(
            items.Select(PublicAgencyReviewMapping.ToResponse).ToArray(), page, size, count));
    }
}

public sealed class CreateAgencyReviewCommandHandler : IRequestHandler<CreateAgencyReviewCommand, Result<PublicAgencyReviewResponse>>
{
    private readonly IPackageBookingRequestRepository _bookings;
    private readonly IAgencyReviewRepository _reviews;
    public CreateAgencyReviewCommandHandler(IPackageBookingRequestRepository bookings, IAgencyReviewRepository reviews)
    {
        _bookings = bookings;
        _reviews = reviews;
    }

    public async Task<Result<PublicAgencyReviewResponse>> Handle(CreateAgencyReviewCommand request, CancellationToken cancellationToken)
    {
        var booking = await _bookings.GetByIdForTravelerAsync(request.BookingRequestId, request.TravelerUserId, cancellationToken);
        if (booking is null || booking.TravelPackage.TravelAgencyId != request.AgencyId ||
            booking.Status != PackageBookingRequestStatus.Accepted || booking.TravelPackage.EndDate.Date >= DateTime.UtcNow.Date)
            return Result<PublicAgencyReviewResponse>.Failure(PackageBookingErrors.ReviewNotAllowed);
        if (booking.AgencyReview is not null)
            return Result<PublicAgencyReviewResponse>.Failure(PackageBookingErrors.AlreadyReviewed);
        var review = new AgencyReview
        {
            Id = Guid.NewGuid(),
            TravelAgencyId = request.AgencyId,
            TravelerUserId = request.TravelerUserId,
            PackageBookingRequestId = booking.Id,
            PackageBookingRequest = booking,
            TravelerUser = booking.TravelerUser,
            Rating = request.Rating,
            Comment = request.Comment.Trim(),
            IsPublished = true,
            CreatedAt = DateTime.UtcNow
        };
        await _reviews.AddAsync(review, cancellationToken);
        await _reviews.SaveChangesAsync(cancellationToken);
        return Result<PublicAgencyReviewResponse>.Success(PublicAgencyReviewMapping.ToResponse(review));
    }
}

public sealed class RespondToAgencyReviewCommandHandler
    : IRequestHandler<RespondToAgencyReviewCommand, Result<PublicAgencyReviewResponse>>
{
    private readonly ITravelAgencyRepository _agencies;
    private readonly IAgencyReviewRepository _reviews;
    public RespondToAgencyReviewCommandHandler(ITravelAgencyRepository agencies, IAgencyReviewRepository reviews)
    {
        _agencies = agencies;
        _reviews = reviews;
    }

    public async Task<Result<PublicAgencyReviewResponse>> Handle(RespondToAgencyReviewCommand request, CancellationToken cancellationToken)
    {
        var agency = await _agencies.GetByUserIdAsync(request.AgencyUserId, cancellationToken: cancellationToken);
        if (agency is null) return Result<PublicAgencyReviewResponse>.Failure(TravelAgencyErrors.AgencyAccountRequired);
        var review = await _reviews.GetForAgencyAsync(agency.Id, request.ReviewId, cancellationToken);
        if (review is null) return Result<PublicAgencyReviewResponse>.Failure(TravelAgencyErrors.NotFound);
        review.AgencyResponse = request.Response.Trim();
        review.AgencyRespondedAt = DateTime.UtcNow;
        review.UpdatedAt = DateTime.UtcNow;
        await _reviews.SaveChangesAsync(cancellationToken);
        return Result<PublicAgencyReviewResponse>.Success(PublicAgencyReviewMapping.ToResponse(review));
    }
}
