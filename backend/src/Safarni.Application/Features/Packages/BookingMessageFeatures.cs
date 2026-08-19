using FluentValidation;
using MediatR;
using Safarni.Application.Abstractions.Notifications;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;
using Safarni.Domain.Entities;
using Safarni.Domain.Enums;

namespace Safarni.Application.Features.Packages;

public sealed record PackageBookingMessageResponse(
    Guid Id,
    Guid BookingRequestId,
    Guid SenderUserId,
    string SenderName,
    string Body,
    DateTime CreatedAt,
    bool IsMine);

public sealed record ListBookingMessagesQuery(Guid UserId, Guid RequestId)
    : IRequest<Result<IReadOnlyList<PackageBookingMessageResponse>>>;

public sealed record SendBookingMessageCommand(Guid UserId, Guid RequestId, string Body)
    : IRequest<Result<PackageBookingMessageResponse>>;

public sealed class SendBookingMessageCommandValidator
    : AbstractValidator<SendBookingMessageCommand>
{
    public SendBookingMessageCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.RequestId).NotEmpty();
        RuleFor(command => command.Body).NotEmpty().MaximumLength(4000);
    }
}

internal static class BookingMessageMapping
{
    public static PackageBookingMessageResponse ToResponse(PackageBookingMessage message, Guid currentUserId) => new(
        message.Id,
        message.PackageBookingRequestId,
        message.SenderUserId,
        message.SenderUser.FullName,
        message.Body,
        message.CreatedAt,
        message.SenderUserId == currentUserId);
}

internal static class BookingParticipantLoader
{
    public static async Task<PackageBookingRequest?> LoadAsync(
        Guid userId,
        Guid requestId,
        IPackageBookingRequestRepository requests,
        ITravelAgencyRepository agencies,
        CancellationToken cancellationToken)
    {
        var travelerRequest = await requests.GetByIdForTravelerAsync(requestId, userId, cancellationToken);
        if (travelerRequest is not null) return travelerRequest;
        var agency = await agencies.GetByUserIdAsync(userId, cancellationToken: cancellationToken);
        return agency is null ? null : await requests.GetByIdForAgencyAsync(requestId, agency.Id, cancellationToken);
    }
}

public sealed class ListBookingMessagesQueryHandler
    : IRequestHandler<ListBookingMessagesQuery, Result<IReadOnlyList<PackageBookingMessageResponse>>>
{
    private readonly IPackageBookingRequestRepository _requests;
    private readonly ITravelAgencyRepository _agencies;

    public ListBookingMessagesQueryHandler(IPackageBookingRequestRepository requests, ITravelAgencyRepository agencies)
    {
        _requests = requests; _agencies = agencies;
    }

    public async Task<Result<IReadOnlyList<PackageBookingMessageResponse>>> Handle(ListBookingMessagesQuery query, CancellationToken cancellationToken)
    {
        var request = await BookingParticipantLoader.LoadAsync(query.UserId, query.RequestId, _requests, _agencies, cancellationToken);
        if (request is null) return Result<IReadOnlyList<PackageBookingMessageResponse>>.Failure(PackageBookingErrors.NotFound);
        var messages = await _requests.ListMessagesAsync(request.Id, cancellationToken);
        return Result<IReadOnlyList<PackageBookingMessageResponse>>.Success(messages.Select(message => BookingMessageMapping.ToResponse(message, query.UserId)).ToArray());
    }
}

public sealed class SendBookingMessageCommandHandler
    : IRequestHandler<SendBookingMessageCommand, Result<PackageBookingMessageResponse>>
{
    private readonly IPackageBookingRequestRepository _requests;
    private readonly ITravelAgencyRepository _agencies;
    private readonly IMarketplaceNotificationService _notifications;

    public SendBookingMessageCommandHandler(
        IPackageBookingRequestRepository requests,
        ITravelAgencyRepository agencies,
        IMarketplaceNotificationService notifications)
    {
        _requests = requests; _agencies = agencies; _notifications = notifications;
    }

    public async Task<Result<PackageBookingMessageResponse>> Handle(SendBookingMessageCommand command, CancellationToken cancellationToken)
    {
        var request = await BookingParticipantLoader.LoadAsync(command.UserId, command.RequestId, _requests, _agencies, cancellationToken);
        if (request is null) return Result<PackageBookingMessageResponse>.Failure(PackageBookingErrors.NotFound);
        var now = DateTime.UtcNow;
        var message = new PackageBookingMessage
        {
            Id = Guid.NewGuid(),
            PackageBookingRequestId = request.Id,
            PackageBookingRequest = request,
            SenderUserId = command.UserId,
            SenderUser = request.TravelerUserId == command.UserId ? request.TravelerUser : request.TravelPackage.TravelAgency.User,
            Body = command.Body.Trim(),
            CreatedAt = now
        };
        await _requests.AddMessageAsync(message, cancellationToken);
        await _requests.SaveChangesAsync(cancellationToken);

        var recipientId = request.TravelerUserId == command.UserId
            ? request.TravelPackage.TravelAgency.UserId
            : request.TravelerUserId;
        await _notifications.NotifyUserAsync(
            recipientId,
            MarketplaceNotificationType.PackageBookingMessageReceived,
            "New booking message",
            $"You received a new message about {request.TravelPackage.Title}.",
            request.TravelerUserId == command.UserId ? "/agency/booking-requests" : "/agency-trips",
            cancellationToken);
        return Result<PackageBookingMessageResponse>.Success(BookingMessageMapping.ToResponse(message, command.UserId));
    }
}
