namespace Safarni.Application.Common.Errors;

public static class TravelAgencyErrors
{
    public static readonly Error CompanyNameAlreadyExists = new(
        "TravelAgency.CompanyNameAlreadyExists",
        "A Travel Agency with the same company name already exists.",
        ErrorType.Conflict);

    public static readonly Error NotFound = new(
        "TravelAgency.NotFound",
        "The Travel Agency profile could not be found.",
        ErrorType.NotFound);

    public static readonly Error DocumentNotFound = new(
        "TravelAgency.DocumentNotFound",
        "The requested verification document could not be found.",
        ErrorType.NotFound);

    public static readonly Error InvalidDocument = new(
        "TravelAgency.InvalidDocument",
        "The verification document is invalid or does not match its declared file type.",
        ErrorType.Validation);

    public static readonly Error DocumentAccessDenied = new(
        "TravelAgency.DocumentAccessDenied",
        "You are not allowed to access this private verification document.",
        ErrorType.Forbidden);

    public static readonly Error RequiredDocumentsMissing = new(
        "TravelAgency.RequiredDocumentsMissing",
        "All required verification documents must be uploaded, valid, and individually approved before the agency can be approved.",
        ErrorType.Conflict);

    public static readonly Error EmailVerificationRequired = new(
        "TravelAgency.EmailVerificationRequired",
        "Verify your email address before completing the company profile.",
        ErrorType.Forbidden);

    public static readonly Error OnboardingAlreadyCompleted = new(
        "TravelAgency.OnboardingAlreadyCompleted",
        "The Travel Agency company profile has already been created.",
        ErrorType.Conflict);

    public static readonly Error InvalidVerificationTransition = new(
        "TravelAgency.InvalidVerificationTransition",
        "The requested verification status transition is not allowed.",
        ErrorType.Conflict);

    public static readonly Error VerifiedAgencyRequired = new(
        "TravelAgency.VerifiedAgencyRequired",
        "Only a verified Travel Agency can publish travel packages.",
        ErrorType.Forbidden);

    public static readonly Error AgencyAccountRequired = new(
        "TravelAgency.AccountRequired",
        "A travel agency account is required for this operation.",
        ErrorType.Forbidden);
}
