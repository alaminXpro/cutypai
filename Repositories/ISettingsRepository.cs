using cutypai.Models;

namespace cutypai.Repositories;

public interface ISettingsRepository
{
    Task<Settings?> GetActiveSettingsAsync(CancellationToken ct = default);
    Task<Settings?> UpdateSettingsAsync(Settings settings, CancellationToken ct = default);
    Task<Settings> CreateDefaultSettingsAsync(CancellationToken ct = default);
}
