using cutypai.Models;

namespace cutypai.Services;

public interface ILipsyncService
{
    Task<LipsyncData?> GenerateLipsyncDataAsync(string audioFilePath, CancellationToken ct = default);
}
