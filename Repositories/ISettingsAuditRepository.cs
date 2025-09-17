using cutypai.Models;

namespace cutypai.Repositories;

public interface ISettingsAuditRepository
{
    Task<List<SettingsAudit>> GetAuditHistoryAsync(string settingsId, int limit = 50);
    Task<List<SettingsAudit>> GetAuditHistoryByUserAsync(string userId, int limit = 50);
    Task<SettingsAudit?> GetLatestAuditAsync(string settingsId);
    Task SaveAuditAsync(SettingsAudit audit);
    Task<List<SettingsAudit>> GetAllAuditHistoryAsync(int limit = 100);
}
