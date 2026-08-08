using Safarni.Domain.Enums;

namespace Safarni.Application.Features.Auth.Register;

public sealed record RegisterUserResponse(
    Guid Id,
    string FullName,
    string Email,
    DateTime CreatedAt,
    bool IsActive,
    RegistrationAccountType AccountType,
    RegisteredTravelAgencyResponse? Agency);

public sealed record RegisteredTravelAgencyResponse(
    Guid Id,
    string CompanyName,
    AgencyVerificationStatus VerificationStatus);
