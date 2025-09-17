using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using cutypai.Models;

namespace cutypai.Services;

public interface IConversationHistoryService
{
    Task<List<ConversationMessage>> GetRecentMessagesAsync(string userId, int maxCount = 5);
    Task SaveMessageAsync(string userId, string message, string? userMood = null, bool isUserMessage = true);
    Task<string> BuildConversationHistoryContextAsync(string userId, int maxHistoryLength = 5);
    Task CleanupOldMessagesAsync(string userId, int keepRecentCount = 10);
}

public class ConversationMessage
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string Message { get; set; } = string.Empty;
    public string? UserMood { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool IsUserMessage { get; set; }
    public string UserId { get; set; } = string.Empty;
}

public class ConversationHistoryService : IConversationHistoryService
{
    private readonly IMongoCollection<ConversationMessage> _collection;
    private readonly ILogger<ConversationHistoryService> _logger;

    public ConversationHistoryService(MongoCollectionFactory factory, ILogger<ConversationHistoryService> logger)
    {
        _collection = factory.GetCollection<ConversationMessage>("conversation_history");
        _logger = logger;
    }

    public async Task<List<ConversationMessage>> GetRecentMessagesAsync(string userId, int maxCount = 5)
    {
        try
        {
            var filter = Builders<ConversationMessage>.Filter.Eq(m => m.UserId, userId);
            var messages = await _collection
                .Find(filter)
                .SortByDescending(m => m.Timestamp)
                .Limit(maxCount)
                .ToListAsync();

            return messages.OrderBy(m => m.Timestamp).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conversation history for user {UserId}", userId);
            return new List<ConversationMessage>();
        }
    }

    public async Task SaveMessageAsync(string userId, string message, string? userMood = null, bool isUserMessage = true)
    {
        try
        {
            var conversationMessage = new ConversationMessage
            {
                Message = message,
                UserMood = userMood,
                IsUserMessage = isUserMessage,
                UserId = userId,
                Timestamp = DateTime.UtcNow
            };

            await _collection.InsertOneAsync(conversationMessage);
            _logger.LogDebug("Saved conversation message for user {UserId}", userId);

            // Cleanup old messages to keep only recent ones (run in background)
            _ = Task.Run(async () => await CleanupOldMessagesAsync(userId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving conversation message for user {UserId}", userId);
        }
    }

    public async Task<string> BuildConversationHistoryContextAsync(string userId, int maxHistoryLength = 5)
    {
        try
        {
            var recentMessages = await GetRecentMessagesAsync(userId, maxHistoryLength);

            if (!recentMessages.Any())
            {
                return "This is the start of your conversation with Cutypai.";
            }

            var context = new List<string> { "Recent conversation context:" };

            foreach (var msg in recentMessages)
            {
                var speaker = msg.IsUserMessage ? "User" : "Cutypai";
                var moodInfo = !string.IsNullOrWhiteSpace(msg.UserMood) ? $" (mood: {msg.UserMood})" : "";
                context.Add($"{speaker}{moodInfo}: {msg.Message}");
            }

            return string.Join("\n", context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building conversation history context for user {UserId}", userId);
            return "Unable to load conversation history.";
        }
    }

    public async Task CleanupOldMessagesAsync(string userId, int keepRecentCount = 10)
    {
        try
        {
            // Get all messages for the user, sorted by timestamp descending
            var allMessages = await _collection
                .Find(m => m.UserId == userId)
                .SortByDescending(m => m.Timestamp)
                .ToListAsync();

            // If we have more than keepRecentCount messages, delete the old ones
            if (allMessages.Count > keepRecentCount)
            {
                var messagesToDelete = allMessages.Skip(keepRecentCount).ToList();
                var idsToDelete = messagesToDelete.Select(m => m.Id).ToList();

                // Delete old messages using their ObjectIds
                var deleteFilter = Builders<ConversationMessage>.Filter.In(m => m.Id, idsToDelete);

                var result = await _collection.DeleteManyAsync(deleteFilter);
                _logger.LogDebug("Cleaned up {DeletedCount} old messages for user {UserId}", result.DeletedCount, userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up old messages for user {UserId}", userId);
        }
    }
}
