using cutypai.Models;

namespace cutypai.Services;

public interface ISettingsAuditService
{
    Task LogSettingsChangeAsync(Settings oldSettings, Settings newSettings, string userId, string userName, string action, string? ipAddress = null, string? userAgent = null);
    Task<List<SettingsAudit>> GetSettingsHistoryAsync(string settingsId, int limit = 20);
    Task<List<SettingsAudit>> GetUserSettingsHistoryAsync(string userId, int limit = 20);
    Task<SettingsAudit?> GetLatestChangeAsync(string settingsId);
}
