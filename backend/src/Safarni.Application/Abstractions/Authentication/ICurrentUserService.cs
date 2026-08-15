namespace Safarni.Application.Abstractions.Authentication;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    
    string SessionId { get; }

    bool IsAuthenticated { get; }
}
