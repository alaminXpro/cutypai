using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace cutypai.Models;

public sealed class RefreshToken
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("token")] public string Token { get; set; } = string.Empty;

    [BsonElement("user_id")] public string UserId { get; set; } = string.Empty;

    [BsonElement("expires_at")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime ExpiresAtUtc { get; set; }

    [BsonElement("created_at")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [BsonElement("revoked_at")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? RevokedAtUtc { get; set; }

    [BsonElement("replaced_by")] public string? ReplacedByToken { get; set; }

    [BsonElement("reason_revoked")] public string? ReasonRevoked { get; set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc;
    public bool IsRevoked => RevokedAtUtc != null;
    public bool IsActive => !IsRevoked && !IsExpired;
}