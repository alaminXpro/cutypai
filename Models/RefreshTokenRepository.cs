using System.Security.Cryptography;
using MongoDB.Driver;

namespace cutypai.Models;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> CreateAsync(string userId, DateTime expiresAt, CancellationToken ct = default);
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct = default);
    Task<bool> RevokeTokenAsync(string token, string reason = "Revoked", CancellationToken ct = default);

    Task<bool> RevokeAllUserTokensAsync(string userId, string reason = "All tokens revoked",
        CancellationToken ct = default);

    Task<bool> DeleteExpiredTokensAsync(CancellationToken ct = default);
}

public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly IMongoCollection<RefreshToken> _collection;

    public RefreshTokenRepository(MongoCollectionFactory factory)
    {
        _collection = factory.GetCollection<RefreshToken>("refresh_tokens");
    }

    public async Task<RefreshToken?> CreateAsync(string userId, DateTime expiresAt, CancellationToken ct = default)
    {
        var token = new RefreshToken
        {
            Token = GenerateSecureToken(),
            UserId = userId,
            ExpiresAtUtc = expiresAt,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _collection.InsertOneAsync(token, cancellationToken: ct);
        return token;
    }

    public Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct = default)
    {
        return _collection.Find(rt => rt.Token == token).FirstOrDefaultAsync(ct);
    }

    public async Task<bool> RevokeTokenAsync(string token, string reason = "Revoked", CancellationToken ct = default)
    {
        var update = Builders<RefreshToken>.Update
            .Set(rt => rt.RevokedAtUtc, DateTime.UtcNow)
            .Set(rt => rt.ReasonRevoked, reason);

        var result = await _collection.UpdateOneAsync(
            rt => rt.Token == token,
            update,
            cancellationToken: ct);

        return result.IsAcknowledged && result.ModifiedCount == 1;
    }

    public async Task<bool> RevokeAllUserTokensAsync(string userId, string reason = "All tokens revoked",
        CancellationToken ct = default)
    {
        var update = Builders<RefreshToken>.Update
            .Set(rt => rt.RevokedAtUtc, DateTime.UtcNow)
            .Set(rt => rt.ReasonRevoked, reason);

        var result = await _collection.UpdateManyAsync(
            rt => rt.UserId == userId && rt.RevokedAtUtc == null,
            update,
            cancellationToken: ct);

        return result.IsAcknowledged;
    }

    public async Task<bool> DeleteExpiredTokensAsync(CancellationToken ct = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-30); // Keep for 30 days for audit
        var result = await _collection.DeleteManyAsync(
            rt => rt.ExpiresAtUtc < cutoffDate,
            ct);

        return result.IsAcknowledged;
    }

    private static string GenerateSecureToken()
    {
        using var rng = RandomNumberGenerator.Create();
        var tokenBytes = new byte[64];
        rng.GetBytes(tokenBytes);
        return Convert.ToBase64String(tokenBytes);
    }
}