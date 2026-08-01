using ChatBotApi.Application.Common.Interfaces;
using ChatBotApi.Infrastructure.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Safarni.Infrastructure;

public static class GeminiItineraryDependencyInjection
{
    /// <summary>
    /// Replaces ONLY the itinerary AI provider.
    /// IAIProvider is deliberately untouched, so Safarni chat remains on Groq.
    /// Call this after the existing Safarni/AI registrations and before Build().
    /// </summary>
    public static IServiceCollection UseGeminiForItineraryGeneration(
        this IServiceCollection services)
    {
        services.RemoveAll<IItineraryAIProvider>();
        services.AddScoped<
            IItineraryAIProvider,
            GeminiItineraryAIProvider>();

        return services;
    }
}
