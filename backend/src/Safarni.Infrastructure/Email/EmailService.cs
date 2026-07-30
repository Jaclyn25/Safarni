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
        message.Subject = "Safarni - Password reset";
        message.Body = new TextPart("html")
        {
            Text = $"<p>Click the link below to reset your password:</p><p><a href=\"{resetLink}\">Reset password</a></p>"
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
        message.Subject = "Safarni - Verify your email";
        message.Body = new TextPart("html")
        {
            Text = $"<p>Click the link below to verify your email address:</p><p><a href=\"{verificationLink}\">Verify email</a></p>"
        };

        return SendAsync(message, verificationLink, cancellationToken);
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
