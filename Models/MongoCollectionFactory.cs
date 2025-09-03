using MongoDB.Driver;

namespace cutypai.Models;

public sealed class MongoCollectionFactory
{
    private readonly IMongoDatabase _db;

    public MongoCollectionFactory(IMongoDatabase db)
    {
        _db = db;
    }

    public IMongoCollection<T> GetCollection<T>(string? name = null)
    {
        return _db.GetCollection<T>(name ?? typeof(T).Name.ToLowerInvariant() + "s");
    }
}