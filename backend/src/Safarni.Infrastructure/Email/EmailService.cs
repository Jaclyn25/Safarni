using System.Net;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Safarni.Application.Abstractions.Security;

namespace Safarni.Infrastructure.Email;

public sealed class EmailService : IEmailService
{
    internal const string AccentColor = "#0f766e";
    internal const string AccentColorDark = "#0b5c55";
    internal const string MutedTextColor = "#6b7280";
    internal const string BodyBackgroundColor = "#f4f6f8";
    internal const string CardBackgroundColor = "#ffffff";

    private readonly EmailSettings _settings;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        IOptions<EmailSettings> options,
        IHostEnvironment environment,
        ILogger<EmailService> logger)
    {
        _settings = options.Value;
        _environment = environment;
        _logger = logger;
    }

    public Task SendPasswordResetAsync(
        string toEmail,
        string resetLink,
        CancellationToken cancellationToken = default)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.DisplayName, _settings.From));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = "Safarni - Reset your password";

        var body =
            $"<p>We received a request to reset the password for your Safarni account.</p>" +
            $"<p>If you made this request, click the button below to choose a new password.</p>" +
            Button(resetLink, "Reset Password") +
            FallbackLink(resetLink, "reset your password");

        message.Body = new TextPart("html")
        {
            Text = BuildEmailShell(
                "Reset your password",
                body)
        };

        return SendAsync(message, resetLink, cancellationToken);
    }

    public Task SendEmailVerificationAsync(
        string toEmail,
        string verificationLink,
        CancellationToken cancellationToken = default)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.DisplayName, _settings.From));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = "Welcome to Safarni - verify your email";

        var body =
            $"<p>Welcome to Safarni! We're excited to help you plan your next journey.</p>" +
            $"<p>Please confirm your email address by clicking the button below. " +
            $"This link is valid for 24 hours.</p>" +
            Button(verificationLink, "Verify Email") +
            FallbackLink(verificationLink, "verify your email");

        message.Body = new TextPart("html")
        {
            Text = BuildEmailShell(
                "Welcome to Safarni",
                body)
        };

        return SendAsync(message, verificationLink, cancellationToken);
    }

    public Task SendItinerarySummaryAsync(
        string toEmail,
        string travelerName,
        string tripName,
        string destination,
        DateTime startDate,
        DateTime endDate,
        string summaryHtml,
        string itineraryLink,
        CancellationToken cancellationToken = default)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.DisplayName, _settings.From));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = $"Safarni - Your itinerary for {tripName} is ready";

        var durationDays = Math.Max(
            1,
            (endDate.Date - startDate.Date).Days + 1);

        var body =
            $"<p>Hi {WebUtility.HtmlEncode(travelerName)},</p>" +
            $"<p>Your itinerary for <strong>{WebUtility.HtmlEncode(tripName)}</strong> is ready. " +
            "Everything is saved in your planner, so you can review, adjust, and take it with you.</p>" +
            DetailsBox(
                ("Destination", destination),
                ("Trip dates", $"{startDate:ddd, MMM d, yyyy} - {endDate:ddd, MMM d, yyyy}"),
                ("Duration", $"{durationDays} day{(durationDays == 1 ? string.Empty : "s")}")) +
            BuildHighlightsSection(summaryHtml) +
            Button(itineraryLink, "View Itinerary") +
            FallbackLink(itineraryLink, "open your itinerary");

        message.Body = new TextPart("html")
        {
            Text = BuildEmailShell(
                "Your itinerary is ready",
                body)
        };

        return SendAsync(message, itineraryLink, cancellationToken);
    }

    internal static string BuildHighlightsSection(string summaryHtml)
    {
        if (string.IsNullOrWhiteSpace(summaryHtml))
        {
            return "<p>Open your trip in the Planner to explore the full day-by-day plan.</p>";
        }

        return
            "<p>Here are a few highlights from your plan:</p>" +
            summaryHtml;
    }

    internal static string DetailsBox(params (string Label, string Value)[] details)
    {
        var rows = string.Concat(details
            .Select(detail =>
                $"<tr>" +
                $"<td style=\"padding:6px 0;color:{MutedTextColor};white-space:nowrap;vertical-align:top;\">{detail.Label}</td>" +
                $"<td style=\"padding:6px 0;padding-left:16px;font-weight:600;vertical-align:top;\">{WebUtility.HtmlEncode(detail.Value)}</td>" +
                $"</tr>"));

        return
            "<table role=\"presentation\" cellpadding=\"0\" cellspacing=\"0\" " +
            $"style=\"margin:16px 0;background:{BodyBackgroundColor};border-radius:8px;padding:12px 16px;\">" +
            rows +
            "</table>";
    }

    internal static string Button(string link, string label)
    {
        return
            "<table role=\"presentation\" cellpadding=\"0\" cellspacing=\"0\" style=\"margin:24px 0;\">" +
            "<tr><td>" +
            $"<a href=\"{link}\" " +
            $"style=\"display:inline-block;background:{AccentColor};color:#ffffff;font-weight:600;" +
            "font-size:14px;padding:13px 30px;border-radius:8px;text-decoration:none;\">" +
            $"{WebUtility.HtmlEncode(label)}</a>" +
            "</td></tr></table>";
    }

    internal static string FallbackLink(string link, string actionLabel)
    {
        return
            "<p style=\"margin-top:20px;font-size:12px;color:" + MutedTextColor + ";\">" +
            $"If the button does not work, copy and paste this link into your browser to {actionLabel}:" +
            $"<br /><a href=\"{link}\" style=\"color:{AccentColor};\">{link}</a>" +
            "</p>";
    }

    internal static string BuildEmailShell(string heading, string bodyHtml)
    {
        return
            "<!DOCTYPE html>" +
            "<html lang=\"en\">" +
            "<head>" +
            "<meta charset=\"utf-8\" />" +
            "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />" +
            "</head>" +
            "<body style=\"margin:0;padding:0;background:" + BodyBackgroundColor + ";\">" +
            "<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" " +
            "style=\"background:" + BodyBackgroundColor + ";\">" +
            "<tr><td align=\"center\" style=\"padding:32px 16px;\">" +
            "<table role=\"presentation\" width=\"560\" cellpadding=\"0\" cellspacing=\"0\" " +
            "style=\"max-width:560px;width:100%;\">" +
            "<tr>" +
            "<td align=\"center\" style=\"padding-bottom:20px;\">" +
            $"<span style=\"font-size:22px;font-weight:800;letter-spacing:2px;color:{AccentColorDark};\">SAFARNI</span>" +
            "</td>" +
            "</tr>" +
            "<tr>" +
            "<td style=\"background:" + CardBackgroundColor + ";border-radius:12px;padding:36px;" +
            "font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif;" +
            "font-size:15px;line-height:1.6;color:#1f2937;\">" +
            $"<h1 style=\"margin:0 0 16px;font-size:22px;color:#111827;\">{WebUtility.HtmlEncode(heading)}</h1>" +
            bodyHtml +
            "</td>" +
            "</tr>" +
            "<tr>" +
            "<td align=\"center\" style=\"padding-top:20px;font-family:Arial,sans-serif;font-size:12px;" +
            "color:" + MutedTextColor + ";\">" +
            "You received this email because of your activity on Safarni." +
            "</td>" +
            "</tr>" +
            "</table>" +
            "</td></tr>" +
            "</table>" +
            "</body>" +
            "</html>";
    }

    private async Task SendAsync(
        MimeMessage message,
        string fallbackLink,
        CancellationToken cancellationToken)
    {
        if (!IsConfigured())
        {
            const string warning =
                "EmailSettings are not configured. Set EmailSettings__From, " +
                "EmailSettings__UserName, and EmailSettings__Password to send real emails.";

            if (_environment.IsDevelopment())
            {
                _logger.LogWarning("{Warning} Development link: {Link}", warning, fallbackLink);
                return;
            }

            throw new InvalidOperationException(warning);
        }

        using var client = new SmtpClient();
        await client.ConnectAsync(
            _settings.Host,
            _settings.Port,
            _settings.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto,
            cancellationToken);

        await client.AuthenticateAsync(_settings.UserName, _settings.Password, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);

        _logger.LogInformation(
            "Email sent successfully to {Recipient}.",
            string.Join(", ", message.To.Mailboxes.Select(mailbox => mailbox.Address)));
    }

    private bool IsConfigured()
    {
        return IsRealValue(_settings.Host) &&
            _settings.Port > 0 &&
            IsRealValue(_settings.From) &&
            IsRealValue(_settings.UserName) &&
            IsRealValue(_settings.Password);
    }

    private static bool IsRealValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return !value.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(value, "CHANGE_ME", StringComparison.OrdinalIgnoreCase);
    }
}
