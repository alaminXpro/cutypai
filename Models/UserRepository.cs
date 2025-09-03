using MongoDB.Bson;
using MongoDB.Driver;

namespace cutypai.Models;

public interface IUserRepository
{
    Task<List<User>> GetAllAsync(CancellationToken ct = default);
    Task<User?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<User> RegisterAsync(User user, CancellationToken ct = default);
    Task<User?> AuthenticateAsync(string email, string password, CancellationToken ct = default);
    Task<bool> UpdateAsync(User user, CancellationToken ct = default);
    Task<bool> DeleteAsync(string id, CancellationToken ct = default);
    Task<bool> SetLastLoginAsync(string id, DateTime whenUtc, CancellationToken ct = default);
    Task<bool> SetStatusAsync(string id, UserStatus status, CancellationToken ct = default);
}

public sealed class UserRepository : IUserRepository
{
    private readonly IMongoCollection<User> _col;

    public UserRepository(MongoCollectionFactory factory)
    {
        _col = factory.GetCollection<User>("users");
    }

    public Task<List<User>> GetAllAsync(CancellationToken ct = default)
    {
        return _col.Find(FilterDefinition<User>.Empty)
            .SortByDescending(u => u.CreatedAtUtc)
            .ToListAsync(ct);
    }

    public Task<User?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        return _col.Find(u => u.Id == id).FirstOrDefaultAsync(ct);
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        return _col.Find(u => u.Email == email).FirstOrDefaultAsync(ct);
    }

    // Registration with hash
    public async Task<User> RegisterAsync(User user, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(user.Id))
            user.Id = ObjectId.GenerateNewId().ToString();

        user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password); // hash here
        user.CreatedAtUtc = DateTime.UtcNow;

        await _col.InsertOneAsync(user, cancellationToken: ct);
        return user;
    }

    // Authentication: verify password
    public async Task<User?> AuthenticateAsync(string email, string password, CancellationToken ct = default)
    {
        var user = await GetByEmailAsync(email, ct);
        if (user is null) return null;

        var valid = BCrypt.Net.BCrypt.Verify(password, user.Password);
        return valid ? user : null;
    }

    public async Task<bool> UpdateAsync(User user, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(user.Id)) return false;
        var res = await _col.ReplaceOneAsync(u => u.Id == user.Id, user, cancellationToken: ct);
        return res.IsAcknowledged && res.ModifiedCount == 1;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken ct = default)
    {
        var res = await _col.DeleteOneAsync(u => u.Id == id, ct);
        return res.IsAcknowledged && res.DeletedCount == 1;
    }

    public async Task<bool> SetLastLoginAsync(string id, DateTime whenUtc, CancellationToken ct = default)
    {
        var update = Builders<User>.Update.Set(u => u.LastLoginUtc, whenUtc);
        var res = await _col.UpdateOneAsync(u => u.Id == id, update, cancellationToken: ct);
        return res.IsAcknowledged && res.ModifiedCount == 1;
    }

    public async Task<bool> SetStatusAsync(string id, UserStatus status, CancellationToken ct = default)
    {
        var update = Builders<User>.Update.Set(u => u.Status, status);
        var res = await _col.UpdateOneAsync(u => u.Id == id, update, cancellationToken: ct);
        return res.IsAcknowledged && res.ModifiedCount == 1;
    }
}