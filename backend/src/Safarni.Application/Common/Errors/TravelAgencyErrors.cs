namespace Safarni.Application.Common.Errors;

public static class TravelAgencyErrors
{
    public static readonly Error RegistrationDetailsRequired = new(
        "TravelAgency.RegistrationDetailsRequired",
        "Travel Agency company details are required.",
        ErrorType.Validation);

    public static readonly Error CompanyNameAlreadyExists = new(
        "TravelAgency.CompanyNameAlreadyExists",
        "A Travel Agency with the same company name already exists.",
        ErrorType.Conflict);
}
