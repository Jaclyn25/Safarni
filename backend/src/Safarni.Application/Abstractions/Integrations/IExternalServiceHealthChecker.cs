namespace Safarni.Application.Abstractions.Integrations;

public sealed record IntegrationHealthCheck(
    string Name,
    string DisplayName,
    string AffectedFeature,
    string Status,
    string Detail,
    bool IsConfigured,
    DateTime CheckedAt,
    long LatencyMs);

public sealed record IntegrationConfigEntry(
    string Integration,
    string Key,
    bool IsConfigured,
    string? MaskedValue);

public interface IExternalServiceHealthChecker
{
    Task<IReadOnlyList<IntegrationHealthCheck>> CheckAllAsync(
        CancellationToken cancellationToken = default);

    Task<IntegrationHealthCheck?> CheckByNameAsync(
        string name,
        CancellationToken cancellationToken = default);

    IReadOnlyList<IntegrationConfigEntry> GetConfigStatus();
}
