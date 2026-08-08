namespace Safarni.Application.Abstractions.Security;

public interface ITravelAgencyAuthorizationService
{
    Task<bool> IsVerifiedAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
