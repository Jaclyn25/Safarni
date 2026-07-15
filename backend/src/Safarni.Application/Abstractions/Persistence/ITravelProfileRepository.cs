using Safarni.Domain.Entities;

namespace Safarni.Application.Abstractions.Persistence;

public interface ITravelProfileRepository
{
    Task<TravelPreference?> GetTravelPreferenceByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<PassportInfo?> GetPassportInfoByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task SaveAsync(CancellationToken cancellationToken = default);

    Task AddTravelPreferenceAsync(TravelPreference travelPreference, CancellationToken cancellationToken = default);

    Task AddPassportInfoAsync(PassportInfo passportInfo, CancellationToken cancellationToken = default);
}
