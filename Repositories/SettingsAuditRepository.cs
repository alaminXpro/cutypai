using MongoDB.Driver;
using cutypai.Models;

namespace cutypai.Repositories;

public sealed class SettingsAuditRepository : ISettingsAuditRepository
{
    private readonly IMongoCollection<SettingsAudit> _collection;
    private readonly ILogger<SettingsAuditRepository> _logger;

    public SettingsAuditRepository(MongoCollectionFactory factory, ILogger<SettingsAuditRepository> logger)
    {
        _collection = factory.GetCollection<SettingsAudit>("settings_audit");
        _logger = logger;
    }

    public async Task<List<SettingsAudit>> GetAuditHistoryAsync(string settingsId, int limit = 50)
    {
        try
        {
            return await _collection
                .Find(a => a.SettingsId == settingsId)
                .SortByDescending(a => a.CreatedAt)
                .Limit(limit)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting audit history for settings {SettingsId}", settingsId);
            return new List<SettingsAudit>();
        }
    }

    public async Task<List<SettingsAudit>> GetAuditHistoryByUserAsync(string userId, int limit = 50)
    {
        try
        {
            return await _collection
                .Find(a => a.UserId == userId)
                .SortByDescending(a => a.CreatedAt)
                .Limit(limit)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting audit history for user {UserId}", userId);
            return new List<SettingsAudit>();
        }
    }

    public async Task<SettingsAudit?> GetLatestAuditAsync(string settingsId)
    {
        try
        {
            return await _collection
                .Find(a => a.SettingsId == settingsId)
                .SortByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting latest audit for settings {SettingsId}", settingsId);
            return null;
        }
    }

    public async Task SaveAuditAsync(SettingsAudit audit)
    {
        try
        {
            await _collection.InsertOneAsync(audit);
            _logger.LogInformation("Saved audit record for settings {SettingsId} by user {UserId}", audit.SettingsId, audit.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving audit record for settings {SettingsId}", audit.SettingsId);
        }
    }

    public async Task<List<SettingsAudit>> GetAllAuditHistoryAsync(int limit = 100)
    {
        try
        {
            return await _collection
                .Find(_ => true)
                .SortByDescending(a => a.CreatedAt)
                .Limit(limit)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all audit history");
            return new List<SettingsAudit>();
        }
    }
}
