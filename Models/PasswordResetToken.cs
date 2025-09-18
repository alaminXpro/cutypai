using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace cutypai.Models;

public sealed class PasswordResetToken
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("token")]
    [Required]
    public string Token { get; set; } = string.Empty;

    [BsonElement("user_email")]
    [Required]
    [EmailAddress]
    public string UserEmail { get; set; } = string.Empty;

    [BsonElement("user_id")]
    [Required]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("expires_at")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime ExpiresAtUtc { get; set; }

    [BsonElement("used")]
    public bool Used { get; set; } = false;

    [BsonElement("created_at")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
