namespace Safarni.Application.Abstractions.Security;

public interface IPasswordResetLinkFactory
{
    string Create(string email, string token);
}
