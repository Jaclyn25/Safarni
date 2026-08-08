using MediatR;
using Safarni.Application.Common.Results;
using Safarni.Domain.Enums;

namespace Safarni.Application.Features.Auth.Register;

public sealed record RegisterUserCommand(
    string FullName,
    string Email,
    string Password,
    string ConfirmPassword,
    RegistrationAccountType AccountType = RegistrationAccountType.Traveler,
    string? PhoneNumber = null,
    TravelAgencyRegistrationDetails? Agency = null)
    : IRequest<Result<RegisterUserResponse>>;

public sealed record TravelAgencyRegistrationDetails(
    string CompanyName,
    string Description,
    string Address,
    string City,
    string Country,
    string CompanyPhone,
    string? LogoUrl,
    string? WebsiteUrl);
