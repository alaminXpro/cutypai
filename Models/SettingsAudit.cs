using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace cutypai.Models;

public sealed class SettingsAudit
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("settings_id")]
    public string SettingsId { get; set; } = string.Empty;

    [BsonElement("user_id")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("user_name")]
    public string UserName { get; set; } = string.Empty;

    [BsonElement("action")]
    public string Action { get; set; } = string.Empty; // "created", "updated", "reset"

    [BsonElement("changes")]
    public List<SettingsChange> Changes { get; set; } = new();

    [BsonElement("old_system_prompt")]
    public string? OldSystemPrompt { get; set; }

    [BsonElement("new_system_prompt")]
    public string? NewSystemPrompt { get; set; }

    [BsonElement("old_instructions")]
    public string? OldInstructions { get; set; }

    [BsonElement("new_instructions")]
    public string? NewInstructions { get; set; }

    [BsonElement("created_at")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("ip_address")]
    public string? IpAddress { get; set; }

    [BsonElement("user_agent")]
    public string? UserAgent { get; set; }
}

public sealed class SettingsChange
{
    [BsonElement("field")]
    public string Field { get; set; } = string.Empty; // "SystemPrompt", "Instructions"

    [BsonElement("old_value")]
    public string? OldValue { get; set; }

    [BsonElement("new_value")]
    public string? NewValue { get; set; }

    [BsonElement("change_type")]
    public string ChangeType { get; set; } = string.Empty; // "added", "modified", "removed"

    [BsonElement("change_summary")]
    public string ChangeSummary { get; set; } = string.Empty;
}
