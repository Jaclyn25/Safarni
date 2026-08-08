namespace Safarni.Domain.Enums;

/// <summary>
/// Public registration choices. These values intentionally do not expose the
/// administrative roles that exist on <see cref="UserRole"/>.
/// </summary>
public enum RegistrationAccountType
{
    Traveler = 0,
    TravelAgency = 1
}
