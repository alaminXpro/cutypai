using MongoDB.Driver;
using cutypai.Models;

namespace cutypai.Services;

public interface IDatabaseIndexService
{
    Task CreateIndexesAsync();
}

public class DatabaseIndexService : IDatabaseIndexService
{
    private readonly IMongoCollection<User> _usersCollection;
    private readonly ILogger<DatabaseIndexService> _logger;

    public DatabaseIndexService(MongoCollectionFactory factory, ILogger<DatabaseIndexService> logger)
    {
        _usersCollection = factory.GetCollection<User>("users");
        _logger = logger;
    }

    public async Task CreateIndexesAsync()
    {
        try
        {
            // Email index (unique, case-insensitive) - for login lookups
            await _usersCollection.Indexes.CreateOneAsync(
                new CreateIndexModel<User>(
                    Builders<User>.IndexKeys.Ascending(u => u.Email),
                    new CreateIndexOptions 
                    { 
                        Unique = true, 
                        Name = "ux_email_ci",
                        Collation = new Collation("en", strength: CollationStrength.Secondary)
                    }
                )
            );

            // External providers compound index - for SSO lookups
            await _usersCollection.Indexes.CreateOneAsync(
                new CreateIndexModel<User>(
                    Builders<User>.IndexKeys.Ascending("external_providers.provider")
                        .Ascending("external_providers.external_id"),
                    new CreateIndexOptions { Name = "external_providers_lookup" }
                )
            );

            // Status index - for filtering active users
            await _usersCollection.Indexes.CreateOneAsync(
                new CreateIndexModel<User>(
                    Builders<User>.IndexKeys.Ascending(u => u.Status),
                    new CreateIndexOptions { Name = "status_index" }
                )
            );

            // Created date index - for sorting and pagination
            await _usersCollection.Indexes.CreateOneAsync(
                new CreateIndexModel<User>(
                    Builders<User>.IndexKeys.Descending(u => u.CreatedAtUtc),
                    new CreateIndexOptions { Name = "created_at_desc" }
                )
            );

            _logger.LogInformation("Database indexes created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating database indexes");
            throw;
        }
    }
}
