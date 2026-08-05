namespace Safarni.Application.Abstractions.Security;

public interface IEmailService
{
    Task SendPasswordResetAsync(string toEmail, string resetLink, CancellationToken cancellationToken = default);
    Task SendEmailVerificationAsync(string toEmail, string verificationLink, CancellationToken cancellationToken = default);
    Task SendItinerarySummaryAsync(
        string toEmail,
        string travelerName,
        string tripName,
        string destination,
        DateTime startDate,
        DateTime endDate,
        string summaryHtml,
        string itineraryLink,
        CancellationToken cancellationToken = default);
}
