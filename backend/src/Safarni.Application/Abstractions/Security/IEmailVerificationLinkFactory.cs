namespace Safarni.Application.Abstractions.Security;

public interface IEmailVerificationLinkFactory
{
    string Create(string email, string token);
}
