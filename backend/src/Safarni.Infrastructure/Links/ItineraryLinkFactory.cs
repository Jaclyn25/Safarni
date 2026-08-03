using Microsoft.Extensions.Options;
using Safarni.Application.Abstractions.Security;

namespace Safarni.Infrastructure.Links;

public sealed class ItineraryLinkFactory : IItineraryLinkFactory
{
    private readonly FrontendSettings _settings;

    public ItineraryLinkFactory(IOptions<FrontendSettings> options)
    {
        _settings = options.Value;
    }

    public string Create(Guid tripId)
    {
        var baseUrl = _settings.BaseUrl.TrimEnd('/');

        return $"{baseUrl}/planner/{tripId}";
    }
}
