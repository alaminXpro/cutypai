using cutypai.Models;
using cutypai.Repositories;

namespace cutypai.Services;

public sealed class SettingsAuditService : ISettingsAuditService
{
    private readonly ISettingsAuditRepository _auditRepository;
    private readonly ILogger<SettingsAuditService> _logger;

    public SettingsAuditService(ISettingsAuditRepository auditRepository, ILogger<SettingsAuditService> logger)
    {
        _auditRepository = auditRepository;
        _logger = logger;
    }

    public async Task LogSettingsChangeAsync(Settings oldSettings, Settings newSettings, string userId, string userName, string action, string? ipAddress = null, string? userAgent = null)
    {
        try
        {
            var changes = new List<SettingsChange>();
            var settingsId = newSettings.Id ?? "unknown";

            // Check SystemPrompt changes
            if (oldSettings.SystemPrompt != newSettings.SystemPrompt)
            {
                changes.Add(new SettingsChange
                {
                    Field = "SystemPrompt",
                    OldValue = oldSettings.SystemPrompt,
                    NewValue = newSettings.SystemPrompt,
                    ChangeType = string.IsNullOrEmpty(oldSettings.SystemPrompt) ? "added" : "modified",
                    ChangeSummary = GenerateChangeSummary(oldSettings.SystemPrompt, newSettings.SystemPrompt, "System Prompt")
                });
            }

            // Check Instructions changes
            if (oldSettings.Instructions != newSettings.Instructions)
            {
                changes.Add(new SettingsChange
                {
                    Field = "Instructions",
                    OldValue = oldSettings.Instructions,
                    NewValue = newSettings.Instructions,
                    ChangeType = string.IsNullOrEmpty(oldSettings.Instructions) ? "added" : "modified",
                    ChangeSummary = GenerateChangeSummary(oldSettings.Instructions, newSettings.Instructions, "Instructions")
                });
            }

            // Only log if there are actual changes
            if (changes.Any())
            {
                var audit = new SettingsAudit
                {
                    SettingsId = settingsId,
                    UserId = userId,
                    UserName = userName,
                    Action = action,
                    Changes = changes,
                    OldSystemPrompt = oldSettings.SystemPrompt,
                    NewSystemPrompt = newSettings.SystemPrompt,
                    OldInstructions = oldSettings.Instructions,
                    NewInstructions = newSettings.Instructions,
                    CreatedAt = DateTime.UtcNow,
                    IpAddress = ipAddress,
                    UserAgent = userAgent
                };

                await _auditRepository.SaveAuditAsync(audit);
                _logger.LogInformation("Logged settings change for user {UserId}: {Action} with {ChangeCount} changes", userId, action, changes.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging settings change for user {UserId}", userId);
        }
    }

    public async Task<List<SettingsAudit>> GetSettingsHistoryAsync(string settingsId, int limit = 20)
    {
        return await _auditRepository.GetAuditHistoryAsync(settingsId, limit);
    }

    public async Task<List<SettingsAudit>> GetUserSettingsHistoryAsync(string userId, int limit = 20)
    {
        return await _auditRepository.GetAuditHistoryByUserAsync(userId, limit);
    }

    public async Task<SettingsAudit?> GetLatestChangeAsync(string settingsId)
    {
        return await _auditRepository.GetLatestAuditAsync(settingsId);
    }

    private static string GenerateChangeSummary(string? oldValue, string? newValue, string fieldName)
    {
        if (string.IsNullOrEmpty(oldValue))
        {
            return $"{fieldName} was added ({newValue?.Length ?? 0} characters)";
        }

        if (string.IsNullOrEmpty(newValue))
        {
            return $"{fieldName} was removed (was {oldValue.Length} characters)";
        }

        var oldLength = oldValue.Length;
        var newLength = newValue.Length;
        var lengthChange = newLength - oldLength;

        var lengthChangeText = lengthChange switch
        {
            > 0 => $"increased by {lengthChange} characters",
            < 0 => $"decreased by {Math.Abs(lengthChange)} characters",
            _ => "length unchanged"
        };

        return $"{fieldName} was modified ({lengthChangeText})";
    }
}
