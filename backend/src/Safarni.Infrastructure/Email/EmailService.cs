using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Options;
using Safarni.Application.Abstractions.Security;

namespace Safarni.Infrastructure.Email;

public sealed class EmailService : IEmailService
{
    private readonly EmailSettings _settings;

    public EmailService(IOptions<EmailSettings> options)
    {
        _settings = options.Value;

        Console.WriteLine($"User: {_settings.UserName}");
        Console.WriteLine($"From: {_settings.From}");
        Console.WriteLine($"Host: {_settings.Host}");
        Console.WriteLine($"Port: {_settings.Port}");
    }

    public async Task SendPasswordResetAsync(string toEmail, string resetLink, CancellationToken cancellationToken = default)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.DisplayName, _settings.From));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = "Safarni — Password reset";
        message.Body = new TextPart("html")
        {
            Text = $"<p>Click the link below to reset your password:</p><p><a href=\"{resetLink}\">Reset password</a></p>"
        };

        using var client = new SmtpClient();
        await client.ConnectAsync(
            _settings.Host,
            _settings.Port,
            _settings.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto,
            cancellationToken);
        await client.AuthenticateAsync(_settings.UserName, _settings.Password, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }

    public async Task SendEmailVerificationAsync(string toEmail, string verificationLink, CancellationToken cancellationToken = default)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.DisplayName, _settings.From));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = "Safarni — Verify your email";
        message.Body = new TextPart("html")
        {
            Text = $"<p>Click the link below to verify your email address:</p><p><a href=\"{verificationLink}\">Verify email</a></p>"
        };

        using var client = new SmtpClient();
        await client.ConnectAsync(
            _settings.Host,
            _settings.Port,
            _settings.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto,
            cancellationToken);
        await client.AuthenticateAsync(_settings.UserName, _settings.Password, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}