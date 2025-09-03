using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace cutypai.Models;

public sealed class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("name")]
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [BsonElement("email")]
    [Required]
    [EmailAddress]
    [StringLength(320)]
    public string Email { get; set; } = string.Empty;

    // Stored as "password" in MongoDB
    [BsonElement("password")] [Required] public string Password { get; set; } = string.Empty;

    [BsonElement("role")]
    [BsonRepresentation(BsonType.String)]
    public UserRole Role { get; set; } = UserRole.User;

    [BsonElement("avatar_url")] public string? AvatarUrl { get; set; }

    [BsonElement("created_at")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [BsonElement("last_login")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? LastLoginUtc { get; set; }

    [BsonElement("openai_thread_id")] public string? OpenAIThreadId { get; set; }

    [BsonElement("preferences")] public BsonDocument? Preferences { get; set; }

    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public UserStatus Status { get; set; } = UserStatus.Active;
}