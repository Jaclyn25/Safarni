using Microsoft.Extensions.Options;
using Safarni.Application.Abstractions.Security;

namespace Safarni.Infrastructure.Links;

public sealed class EmailVerificationLinkFactory : IEmailVerificationLinkFactory
{
    private readonly FrontendSettings _settings;

    public EmailVerificationLinkFactory(IOptions<FrontendSettings> options)
    {
        _settings = options.Value;
    }

    public string Create(string email, string token)
    {
        var baseUrl = _settings.BaseUrl.TrimEnd('/');
        var encodedEmail = Uri.EscapeDataString(email);
        var encodedToken = Uri.EscapeDataString(token);

        return $"{baseUrl}/verify-email?email={encodedEmail}&token={encodedToken}";
    }
}
