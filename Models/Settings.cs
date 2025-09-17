using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace cutypai.Models;

public sealed class Settings
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("system_prompt")]
    public string SystemPrompt { get; set; } = string.Empty;

    [BsonElement("instructions")]
    public string Instructions { get; set; } = string.Empty;

    [BsonElement("updated_at")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class SettingsUpdateRequest
{
    public string SystemPrompt { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;
}

public sealed class SettingsResponse
{
    public string SystemPrompt { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
}
