using Safarni.Domain.Entities;
using Safarni.Domain.Enums;

namespace Safarni.Application.Features.Agencies.Common;

public sealed record AgencyVerificationDocumentResponse(
    Guid? Id,
    AgencyVerificationDocumentType DocumentType,
    string DisplayName,
    bool IsRequired,
    bool IsUploaded,
    AgencyDocumentReviewStatus ReviewStatus,
    string? ReviewReason,
    DateTime? ReviewedAt,
    Guid? ReviewedByUserId,
    string? DocumentNumber,
    string? OriginalFileName,
    string? ContentType,
    long? FileSizeBytes,
    DateTime? ExpiryDate,
    DateTime? UploadedAt,
    DateTime? UpdatedAt);

public sealed record AgencyVerificationStatusResponse(
    Guid AgencyId,
    string CompanyName,
    AgencyVerificationStatus VerificationStatus,
    string? DecisionReason,
    DateTime? ReviewedAt,
    bool CanPublishPackages,
    int UploadedDocumentCount,
    int RequiredDocumentCount,
    IReadOnlyList<AgencyVerificationDocumentResponse> Documents);

public sealed record AgencyProfileResponse(
    Guid AgencyId,
    Guid UserId,
    string RepresentativeName,
    string Email,
    string? RepresentativePhone,
    string CompanyName,
    string Description,
    string Address,
    string City,
    string Country,
    string CompanyPhone,
    string? LogoUrl,
    string? WebsiteUrl,
    AgencyVerificationStatus VerificationStatus,
    string? VerificationDecisionReason,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record AgencyVerificationDocumentContentResponse(
    byte[] Content,
    string ContentType,
    string FileName);

public sealed record AgencyDashboardSummaryResponse(
    Guid AgencyId,
    string CompanyName,
    int TotalPackages,
    int PublishedPackages,
    int DraftPackages,
    int TotalBookingRequests,
    int PendingBookingRequests,
    int AcceptedBookingRequests,
    int RejectedBookingRequests,
    int TotalSeatsSold,
    int TotalSeatsAvailable,
    string VerificationStatus,
    string VerificationProgress,
    bool CanPublishPackages,
    DateTime LastUpdated);

public sealed record AgencyDashboardTrendPointResponse(
    string Date,
    int RequestsCount,
    int AcceptedCount,
    int RejectedCount);

public sealed record AgencyActivityItemResponse(
    string Id,
    string Title,
    string Description,
    string Timestamp,
    string Type,
    string? Status);

public static class AgencyResponseMapper
{
    private static readonly AgencyVerificationDocumentType[] RequiredDocumentTypes =
        Enum.GetValues<AgencyVerificationDocumentType>();

    public static AgencyVerificationStatusResponse ToVerificationStatus(TravelAgency agency)
    {
        var byType = agency.VerificationDocuments.ToDictionary(document => document.DocumentType);
        var documents = RequiredDocumentTypes
            .Select(type => ToDocument(type, byType.GetValueOrDefault(type)))
            .ToArray();

        return new AgencyVerificationStatusResponse(
            agency.Id,
            agency.CompanyName,
            agency.VerificationStatus,
            agency.VerificationDecisionReason,
            agency.VerificationReviewedAt,
            agency.VerificationStatus == AgencyVerificationStatus.Verified,
            documents.Count(document => document.IsUploaded),
            documents.Length,
            documents);
    }

    public static AgencyProfileResponse ToProfile(TravelAgency agency)
    {
        return new AgencyProfileResponse(
            agency.Id,
            agency.UserId,
            agency.User.FullName,
            agency.User.Email,
            agency.User.PhoneNumber,
            agency.CompanyName,
            agency.Description,
            agency.Address,
            agency.City,
            agency.Country,
            agency.CompanyPhone,
            agency.LogoUrl,
            agency.WebsiteUrl,
            agency.VerificationStatus,
            agency.VerificationDecisionReason,
            agency.CreatedAt,
            agency.UpdatedAt);
    }

    private static AgencyVerificationDocumentResponse ToDocument(
        AgencyVerificationDocumentType type,
        AgencyVerificationDocument? document)
    {
        return new AgencyVerificationDocumentResponse(
            document?.Id,
            type,
            DisplayName(type),
            true,
            document is not null,
            document?.ReviewStatus ?? AgencyDocumentReviewStatus.Pending,
            document?.ReviewReason,
            document?.ReviewedAt,
            document?.ReviewedByUserId,
            document?.DocumentNumber,
            document?.OriginalFileName,
            document?.ContentType,
            document?.FileSizeBytes,
            document?.ExpiryDate,
            document?.UploadedAt,
            document?.UpdatedAt);
    }

    public static string DisplayName(AgencyVerificationDocumentType type) => type switch
    {
        AgencyVerificationDocumentType.CommercialRegistration => "Commercial Registration",
        AgencyVerificationDocumentType.TaxCard => "Tax Card",
        AgencyVerificationDocumentType.TourismCompanyLicense => "Tourism Company License",
        _ => type.ToString()
    };
}
