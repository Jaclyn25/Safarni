using FluentValidation;
using MediatR;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Abstractions.Storage;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;
using Safarni.Domain.Entities;

namespace Safarni.Application.Features.Packages;

public sealed record BookingTravelerResponse(
    Guid Id,
    Guid BookingRequestId,
    string FullName,
    DateTime? DateOfBirth,
    string Nationality,
    string PassportNumber,
    DateTime? PassportExpiry,
    string Email,
    string Phone,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record BookingDocumentResponse(
    Guid Id,
    Guid BookingRequestId,
    Guid? TravelerId,
    string DocumentType,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    DateTime CreatedAt);

public sealed record ListBookingTravelersQuery(Guid UserId, Guid RequestId)
    : IRequest<Result<IReadOnlyList<BookingTravelerResponse>>>;

public sealed record UpsertBookingTravelerCommand(
    Guid UserId,
    Guid RequestId,
    Guid? TravelerId,
    string FullName,
    DateTime? DateOfBirth,
    string Nationality,
    string PassportNumber,
    DateTime? PassportExpiry,
    string Email,
    string Phone)
    : IRequest<Result<BookingTravelerResponse>>;

public sealed record ListBookingDocumentsQuery(Guid UserId, Guid RequestId)
    : IRequest<Result<IReadOnlyList<BookingDocumentResponse>>>;

public sealed record UploadBookingDocumentCommand(
    Guid UserId,
    Guid RequestId,
    Guid? TravelerId,
    string DocumentType,
    Stream FileStream,
    string OriginalFileName,
    string ContentType,
    long SizeBytes)
    : IRequest<Result<BookingDocumentResponse>>;

public sealed class UpsertBookingTravelerCommandValidator : AbstractValidator<UpsertBookingTravelerCommand>
{
    public UpsertBookingTravelerCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.RequestId).NotEmpty();
        RuleFor(command => command.FullName).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Nationality).NotEmpty().MaximumLength(100);
        RuleFor(command => command.PassportNumber).NotEmpty().MaximumLength(100);
        RuleFor(command => command.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(command => command.Phone).NotEmpty().MaximumLength(40);
    }
}

internal static class BookingTravelerMapping
{
    public static BookingTravelerResponse ToResponse(PackageBookingTraveler traveler) => new(
        traveler.Id, traveler.PackageBookingRequestId, traveler.FullName, traveler.DateOfBirth,
        traveler.Nationality, traveler.PassportNumber, traveler.PassportExpiry,
        traveler.Email, traveler.Phone, traveler.CreatedAt, traveler.UpdatedAt);

    public static BookingDocumentResponse ToResponse(PackageBookingDocument document) => new(
        document.Id, document.PackageBookingRequestId, document.PackageBookingTravelerId,
        document.DocumentType, document.OriginalFileName, document.ContentType,
        document.SizeBytes, document.CreatedAt);
}

public sealed class ListBookingTravelersQueryHandler
    : IRequestHandler<ListBookingTravelersQuery, Result<IReadOnlyList<BookingTravelerResponse>>>
{
    private readonly IPackageBookingRequestRepository _requests;
    private readonly ITravelAgencyRepository _agencies;

    public ListBookingTravelersQueryHandler(IPackageBookingRequestRepository requests, ITravelAgencyRepository agencies)
    { _requests = requests; _agencies = agencies; }

    public async Task<Result<IReadOnlyList<BookingTravelerResponse>>> Handle(ListBookingTravelersQuery query, CancellationToken cancellationToken)
    {
        var request = await BookingParticipantLoader.LoadAsync(query.UserId, query.RequestId, _requests, _agencies, cancellationToken);
        if (request is null) return Result<IReadOnlyList<BookingTravelerResponse>>.Failure(PackageBookingErrors.NotFound);
        var travelers = await _requests.ListTravelersAsync(request.Id, cancellationToken);
        return Result<IReadOnlyList<BookingTravelerResponse>>.Success(travelers.Select(BookingTravelerMapping.ToResponse).ToArray());
    }
}

public sealed class UpsertBookingTravelerCommandHandler
    : IRequestHandler<UpsertBookingTravelerCommand, Result<BookingTravelerResponse>>
{
    private readonly IPackageBookingRequestRepository _requests;
    private readonly ITravelAgencyRepository _agencies;

    public UpsertBookingTravelerCommandHandler(IPackageBookingRequestRepository requests, ITravelAgencyRepository agencies)
    { _requests = requests; _agencies = agencies; }

    public async Task<Result<BookingTravelerResponse>> Handle(UpsertBookingTravelerCommand command, CancellationToken cancellationToken)
    {
        var request = await BookingParticipantLoader.LoadAsync(command.UserId, command.RequestId, _requests, _agencies, cancellationToken);
        if (request is null) return Result<BookingTravelerResponse>.Failure(PackageBookingErrors.NotFound);
        var traveler = command.TravelerId is { } travelerId
            ? await _requests.GetTravelerAsync(request.Id, travelerId, cancellationToken)
            : null;
        if (command.TravelerId is not null && traveler is null)
            return Result<BookingTravelerResponse>.Failure(PackageBookingErrors.NotFound);
        var now = DateTime.UtcNow;
        traveler ??= new PackageBookingTraveler
        {
            Id = Guid.NewGuid(), PackageBookingRequestId = request.Id, PackageBookingRequest = request, CreatedAt = now
        };
        traveler.FullName = command.FullName.Trim();
        traveler.DateOfBirth = command.DateOfBirth;
        traveler.Nationality = command.Nationality.Trim();
        traveler.PassportNumber = command.PassportNumber.Trim();
        traveler.PassportExpiry = command.PassportExpiry;
        traveler.Email = command.Email.Trim();
        traveler.Phone = command.Phone.Trim();
        traveler.UpdatedAt = now;
        if (command.TravelerId is null) await _requests.AddTravelerAsync(traveler, cancellationToken);
        await _requests.SaveChangesAsync(cancellationToken);
        return Result<BookingTravelerResponse>.Success(BookingTravelerMapping.ToResponse(traveler));
    }
}

public sealed class ListBookingDocumentsQueryHandler
    : IRequestHandler<ListBookingDocumentsQuery, Result<IReadOnlyList<BookingDocumentResponse>>>
{
    private readonly IPackageBookingRequestRepository _requests;
    private readonly ITravelAgencyRepository _agencies;

    public ListBookingDocumentsQueryHandler(IPackageBookingRequestRepository requests, ITravelAgencyRepository agencies)
    { _requests = requests; _agencies = agencies; }

    public async Task<Result<IReadOnlyList<BookingDocumentResponse>>> Handle(ListBookingDocumentsQuery query, CancellationToken cancellationToken)
    {
        var request = await BookingParticipantLoader.LoadAsync(query.UserId, query.RequestId, _requests, _agencies, cancellationToken);
        if (request is null) return Result<IReadOnlyList<BookingDocumentResponse>>.Failure(PackageBookingErrors.NotFound);
        var documents = await _requests.ListDocumentsAsync(request.Id, cancellationToken);
        return Result<IReadOnlyList<BookingDocumentResponse>>.Success(documents.Select(BookingTravelerMapping.ToResponse).ToArray());
    }
}

public sealed class UploadBookingDocumentCommandHandler
    : IRequestHandler<UploadBookingDocumentCommand, Result<BookingDocumentResponse>>
{
    private readonly IPackageBookingRequestRepository _requests;
    private readonly ITravelAgencyRepository _agencies;
    private readonly IBookingDocumentStorage _storage;

    public UploadBookingDocumentCommandHandler(
        IPackageBookingRequestRepository requests,
        ITravelAgencyRepository agencies,
        IBookingDocumentStorage storage)
    { _requests = requests; _agencies = agencies; _storage = storage; }

    public async Task<Result<BookingDocumentResponse>> Handle(UploadBookingDocumentCommand command, CancellationToken cancellationToken)
    {
        var request = await BookingParticipantLoader.LoadAsync(command.UserId, command.RequestId, _requests, _agencies, cancellationToken);
        if (request is null) return Result<BookingDocumentResponse>.Failure(PackageBookingErrors.NotFound);
        if (command.TravelerId is { } travelerId && await _requests.GetTravelerAsync(request.Id, travelerId, cancellationToken) is null)
            return Result<BookingDocumentResponse>.Failure(PackageBookingErrors.NotFound);
        if (command.SizeBytes <= 0 || command.SizeBytes > 10 * 1024 * 1024)
            return Result<BookingDocumentResponse>.Failure(PackageBookingErrors.PaymentNotAllowed);

        var extension = Path.GetExtension(command.OriginalFileName);
        var allowed = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".webp" };
        if (!allowed.Contains(extension, StringComparer.OrdinalIgnoreCase))
            return Result<BookingDocumentResponse>.Failure(PackageBookingErrors.PaymentNotAllowed);
        var reference = await _storage.SaveAsync(request.Id, command.FileStream, extension, cancellationToken);
        var document = new PackageBookingDocument
        {
            Id = Guid.NewGuid(), PackageBookingRequestId = request.Id, PackageBookingRequest = request,
            PackageBookingTravelerId = command.TravelerId, DocumentType = command.DocumentType.Trim(),
            StorageReference = reference, OriginalFileName = Path.GetFileName(command.OriginalFileName),
            ContentType = command.ContentType, SizeBytes = command.SizeBytes, CreatedAt = DateTime.UtcNow
        };
        await _requests.AddDocumentAsync(document, cancellationToken);
        await _requests.SaveChangesAsync(cancellationToken);
        return Result<BookingDocumentResponse>.Success(BookingTravelerMapping.ToResponse(document));
    }
}
