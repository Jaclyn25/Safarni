namespace Safarni.Application.Abstractions.Persistence;

public enum AgencyRegistrationPersistenceResult
{
    Created = 0,
    DuplicateEmail = 1,
    DuplicateCompanyName = 2
}
