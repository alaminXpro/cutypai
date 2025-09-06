using MongoDB.Bson;
using MongoDB.Driver;

namespace cutypai.Models;

public interface IUserRepository
{
    Task<List<User>> GetAllAsync(CancellationToken ct = default);
    Task<User?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);

    Task<(User? User, PasswordValidationResult ValidationResult)> RegisterAsync(User user,
        CancellationToken ct = default);

    Task<User?> AuthenticateAsync(string email, string password, CancellationToken ct = default);
    Task<bool> UpdateAsync(User user, CancellationToken ct = default);
    Task<bool> DeleteAsync(string id, CancellationToken ct = default);
    Task<bool> SetLastLoginAsync(string id, DateTime whenUtc, CancellationToken ct = default);
    Task<bool> SetStatusAsync(string id, UserStatus status, CancellationToken ct = default);
    Task<User?> FindByExternalIdAsync(string provider, string externalId, CancellationToken ct = default);
    Task<User> CreateFromSsoAsync(string email, string name, string provider, string externalId, string? avatarUrl = null, CancellationToken ct = default);
}

public sealed class UserRepository : IUserRepository
{
    private readonly IMongoCollection<User> _col;
    private readonly ILogger<UserRepository> _logger;
    private readonly IPasswordValidationService _passwordValidation;

    public UserRepository(MongoCollectionFactory factory, IPasswordValidationService passwordValidation,
        ILogger<UserRepository> logger)
    {
        _col = factory.GetCollection<User>("users");
        _passwordValidation = passwordValidation;
        _logger = logger;
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

    // Enhanced registration with password validation
    public async Task<(User? User, PasswordValidationResult ValidationResult)> RegisterAsync(User user,
        CancellationToken ct = default)
    {
        try
        {
            // Validate password first
            var validationResult = _passwordValidation.ValidatePassword(user.Password);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Password validation failed for user {Email}: {Errors}",
                    user.Email, string.Join(", ", validationResult.Errors));
                return (null, validationResult);
            }

            // Check if user already exists
            var existingUser = await GetByEmailAsync(user.Email, ct);
            if (existingUser != null)
            {
                validationResult.IsValid = false;
                validationResult.Errors.Add("A user with this email already exists");
                return (null, validationResult);
            }

            if (string.IsNullOrWhiteSpace(user.Id))
                user.Id = ObjectId.GenerateNewId().ToString();

            user.Password =
                BCrypt.Net.BCrypt.HashPassword(user.Password, 12); // Increased work factor for better security
            user.CreatedAtUtc = DateTime.UtcNow;

            await _col.InsertOneAsync(user, cancellationToken: ct);

            _logger.LogInformation("User registered successfully: {Email}", user.Email);
            return (user, validationResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering user {Email}", user.Email);
            throw;
        }
    }

    // Enhanced authentication with logging and security measures
    public async Task<User?> AuthenticateAsync(string email, string password, CancellationToken ct = default)
    {
        try
        {
            var user = await GetByEmailAsync(email, ct);
            if (user is null)
            {
                _logger.LogWarning("Authentication failed for {Email}: User not found", email);
                // Still perform BCrypt to prevent timing attacks
                BCrypt.Net.BCrypt.Verify("dummy", "$2a$12$dummy.hash.to.prevent.timing.attacks");
                return null;
            }

            if (user.Status != UserStatus.Active)
            {
                _logger.LogWarning("Authentication failed for {Email}: User account is {Status}", email, user.Status);
                return null;
            }

            var valid = BCrypt.Net.BCrypt.Verify(password, user.Password);
            if (valid)
            {
                _logger.LogInformation("User authenticated successfully: {Email}", email);
                // Update last login time
                await SetLastLoginAsync(user.Id!, DateTime.UtcNow, ct);
                return user;
            }

            _logger.LogWarning("Authentication failed for {Email}: Invalid password", email);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error authenticating user {Email}", email);
            return null;
        }
    }

    public async Task<bool> UpdateAsync(User user, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(user.Id)) return false;
            var res = await _col.ReplaceOneAsync(u => u.Id == user.Id, user, cancellationToken: ct);
            var success = res.IsAcknowledged && res.ModifiedCount == 1;

            if (success)
                _logger.LogInformation("User updated successfully: {UserId}", user.Id);
            else
                _logger.LogWarning("Failed to update user: {UserId}", user.Id);

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", user.Id);
            return false;
        }
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken ct = default)
    {
        try
        {
            var res = await _col.DeleteOneAsync(u => u.Id == id, ct);
            var success = res.IsAcknowledged && res.DeletedCount == 1;

            if (success)
                _logger.LogInformation("User deleted successfully: {UserId}", id);
            else
                _logger.LogWarning("Failed to delete user: {UserId}", id);

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", id);
            return false;
        }
    }

    public async Task<bool> SetLastLoginAsync(string id, DateTime whenUtc, CancellationToken ct = default)
    {
        try
        {
            var update = Builders<User>.Update.Set(u => u.LastLoginUtc, whenUtc);
            var res = await _col.UpdateOneAsync(u => u.Id == id, update, cancellationToken: ct);
            return res.IsAcknowledged && res.ModifiedCount == 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating last login for user {UserId}", id);
            return false;
        }
    }

    public async Task<bool> SetStatusAsync(string id, UserStatus status, CancellationToken ct = default)
    {
        try
        {
            var update = Builders<User>.Update.Set(u => u.Status, status);
            var res = await _col.UpdateOneAsync(u => u.Id == id, update, cancellationToken: ct);
            var success = res.IsAcknowledged && res.ModifiedCount == 1;

            if (success)
                _logger.LogInformation("User status updated to {Status} for user {UserId}", status, id);

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating status for user {UserId}", id);
            return false;
        }
    }

    public async Task<User?> FindByExternalIdAsync(string provider, string externalId, CancellationToken ct = default)
    {
        try
        {
            var filter = Builders<User>.Filter.ElemMatch(
                u => u.ExternalProviders,
                ep => ep.Provider == provider && ep.ExternalId == externalId
            );
            
            return await _col.Find(filter).FirstOrDefaultAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding user by external ID {Provider}:{ExternalId}", provider, externalId);
            return null;
        }
    }

    public async Task<User> CreateFromSsoAsync(string email, string name, string provider, string externalId, string? avatarUrl = null, CancellationToken ct = default)
    {
        try
        {
            var user = new User
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Name = name,
                Email = email,
                AvatarUrl = avatarUrl,
                Role = UserRole.User,
                Status = UserStatus.Active,
                CreatedAtUtc = DateTime.UtcNow,
                LastLoginUtc = DateTime.UtcNow,
                ExternalProviders = new List<ExternalProvider>
                {
                    new() { Provider = provider, ExternalId = externalId, Email = email, LinkedAt = DateTime.UtcNow }
                }
            };

            await _col.InsertOneAsync(user, cancellationToken: ct);
            
            _logger.LogInformation("SSO user created successfully: {Email} via {Provider}", email, provider);
            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating SSO user {Email} via {Provider}", email, provider);
            throw;
        }
    }
}