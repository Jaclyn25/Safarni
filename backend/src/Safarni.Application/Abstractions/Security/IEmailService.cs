namespace Safarni.Application.Abstractions.Security;

public interface IEmailService
{
    Task SendPasswordResetAsync(string toEmail, string resetLink, CancellationToken cancellationToken = default);
    Task SendEmailVerificationAsync(string toEmail, string verificationLink, CancellationToken cancellationToken = default);
}