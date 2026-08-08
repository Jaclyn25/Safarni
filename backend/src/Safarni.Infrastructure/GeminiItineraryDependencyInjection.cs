using ChatBotApi.Application.Common.Interfaces;
using ChatBotApi.Infrastructure.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Safarni.Application.Abstractions.AI;

namespace Safarni.Infrastructure;

public static class GeminiItineraryDependencyInjection
{
    /// <summary>
    /// Replaces the itinerary JSON provider and registers the Gemini
    /// function-calling model used by the end-to-end itinerary agent.
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
        services.AddScoped<
            IItineraryAgentModel,
            GeminiItineraryAgentModel>();

        return services;
    }
}
