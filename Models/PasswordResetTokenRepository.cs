using MongoDB.Driver;
using System.Security.Cryptography;

namespace cutypai.Models;

public interface IPasswordResetTokenRepository
{
    Task<PasswordResetToken> CreateTokenAsync(string userId, string userEmail, CancellationToken ct = default);
    Task<PasswordResetToken?> GetValidTokenAsync(string token, string email, CancellationToken ct = default);
    Task<bool> MarkTokenAsUsedAsync(string id, CancellationToken ct = default);
    Task<bool> DeleteExpiredTokensAsync(CancellationToken ct = default);
    Task<bool> DeleteUserTokensAsync(string userId, CancellationToken ct = default);
}

public sealed class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly IMongoCollection<PasswordResetToken> _col;
    private readonly ILogger<PasswordResetTokenRepository> _logger;

    public PasswordResetTokenRepository(MongoCollectionFactory factory, ILogger<PasswordResetTokenRepository> logger)
    {
        _col = factory.GetCollection<PasswordResetToken>("password_reset_tokens");
        _logger = logger;
    }

    public async Task<PasswordResetToken> CreateTokenAsync(string userId, string userEmail, CancellationToken ct = default)
    {
        // Generate a secure random token
        var tokenBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(tokenBytes);
        }
        var token = Convert.ToBase64String(tokenBytes).Replace("+", "-").Replace("/", "_").Replace("=", "");

        var resetToken = new PasswordResetToken
        {
            Token = token,
            UserId = userId,
            UserEmail = userEmail,
            ExpiresAtUtc = DateTime.UtcNow.AddHours(1), // Token expires in 1 hour
            CreatedAtUtc = DateTime.UtcNow
        };

        await _col.InsertOneAsync(resetToken, cancellationToken: ct);
        
        _logger.LogInformation("Password reset token created for user {UserId}", userId);
        return resetToken;
    }

    public async Task<PasswordResetToken?> GetValidTokenAsync(string token, string email, CancellationToken ct = default)
    {
        var filter = Builders<PasswordResetToken>.Filter.And(
            Builders<PasswordResetToken>.Filter.Eq(t => t.Token, token),
            Builders<PasswordResetToken>.Filter.Eq(t => t.UserEmail, email),
            Builders<PasswordResetToken>.Filter.Eq(t => t.Used, false),
            Builders<PasswordResetToken>.Filter.Gt(t => t.ExpiresAtUtc, DateTime.UtcNow)
        );

        return await _col.Find(filter).FirstOrDefaultAsync(ct);
    }

    public async Task<bool> MarkTokenAsUsedAsync(string id, CancellationToken ct = default)
    {
        var filter = Builders<PasswordResetToken>.Filter.Eq(t => t.Id, id);
        var update = Builders<PasswordResetToken>.Update.Set(t => t.Used, true);

        var result = await _col.UpdateOneAsync(filter, update, cancellationToken: ct);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> DeleteExpiredTokensAsync(CancellationToken ct = default)
    {
        var filter = Builders<PasswordResetToken>.Filter.Lt(t => t.ExpiresAtUtc, DateTime.UtcNow);
        var result = await _col.DeleteManyAsync(filter, ct);
        
        if (result.DeletedCount > 0)
        {
            _logger.LogInformation("Deleted {Count} expired password reset tokens", result.DeletedCount);
        }
        
        return true;
    }

    public async Task<bool> DeleteUserTokensAsync(string userId, CancellationToken ct = default)
    {
        var filter = Builders<PasswordResetToken>.Filter.Eq(t => t.UserId, userId);
        var result = await _col.DeleteManyAsync(filter, ct);
        
        _logger.LogInformation("Deleted {Count} password reset tokens for user {UserId}", result.DeletedCount, userId);
        return true;
    }
}
