namespace Safarni.Application.Abstractions.Travel;

public sealed record TravelCountry(
    string Name,
    string Iso2);

public interface ITravelLocationCatalog
{
    Task<IReadOnlyList<TravelCountry>> GetCountriesAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetCitiesAsync(
        string country,
        CancellationToken cancellationToken = default);
}
