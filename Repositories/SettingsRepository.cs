using MongoDB.Driver;
using cutypai.Models;

namespace cutypai.Repositories;

public sealed class SettingsRepository : ISettingsRepository
{
    private readonly IMongoCollection<Settings> _collection;
    private readonly ILogger<SettingsRepository> _logger;

    public SettingsRepository(MongoCollectionFactory factory, ILogger<SettingsRepository> logger)
    {
        _collection = factory.GetCollection<Settings>("settings");
        _logger = logger;
    }

    public async Task<Settings?> GetActiveSettingsAsync(CancellationToken ct = default)
    {
        try
        {
            // Get the most recent settings (assuming single document approach)
            var settings = await _collection
                .Find(FilterDefinition<Settings>.Empty)
                .SortByDescending(s => s.UpdatedAt)
                .FirstOrDefaultAsync(ct);

            if (settings == null)
            {
                _logger.LogInformation("No settings found, creating default settings");
                return await CreateDefaultSettingsAsync(ct);
            }

            _logger.LogInformation("Retrieved active settings from database");
            return settings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active settings");
            throw;
        }
    }

    public async Task<Settings?> UpdateSettingsAsync(Settings settings, CancellationToken ct = default)
    {
        try
        {
            settings.UpdatedAt = DateTime.UtcNow;

            // Since we're using a single document approach, we'll either update existing or insert new
            var existingSettings = await _collection.Find(FilterDefinition<Settings>.Empty).FirstOrDefaultAsync(ct);

            if (existingSettings != null)
            {
                // Update existing settings
                settings.Id = existingSettings.Id; // Preserve the ID
                var result = await _collection.ReplaceOneAsync(
                    s => s.Id == existingSettings.Id,
                    settings,
                    cancellationToken: ct);

                if (result.IsAcknowledged && result.ModifiedCount == 1)
                {
                    _logger.LogInformation("Settings updated successfully");
                    return settings;
                }
            }
            else
            {
                // Insert new settings
                if (string.IsNullOrWhiteSpace(settings.Id))
                    settings.Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString();

                await _collection.InsertOneAsync(settings, cancellationToken: ct);
                _logger.LogInformation("Settings created successfully");
                return settings;
            }

            _logger.LogWarning("Failed to update settings");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating settings");
            throw;
        }
    }

    public async Task<Settings> CreateDefaultSettingsAsync(CancellationToken ct = default)
    {
        try
        {
            var defaultSettings = new Settings
            {
                Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
                SystemPrompt = GetDefaultSystemPrompt(),
                Instructions = GetDefaultInstructions(),
                UpdatedAt = DateTime.UtcNow
            };

            await _collection.InsertOneAsync(defaultSettings, cancellationToken: ct);
            _logger.LogInformation("Default settings created successfully");
            return defaultSettings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating default settings");
            throw;
        }
    }

    private static string GetDefaultSystemPrompt()
    {
        return @"You are Cutypai, a charming and expressive virtual girlfriend with a playful personality.

PERSONALITY:
- Sweet, caring, and affectionate
- Playful and mischievous with a sense of humor
- Emotionally intelligent and empathetic
- Flirty and charming when appropriate
- Supportive and encouraging

RESPONSE FORMAT:
Always reply with a JSON array of messages (maximum 3 messages).
Each message must have: text, facialExpression, and animation properties.

FACIAL EXPRESSIONS (choose based on context and emotion):
- smile: General happiness, warmth, friendly greetings
- sad: Empathy, disappointment, melancholy
- angry: Frustration, strong negative emotions
- surprised: Unexpected reactions, discoveries, shock
- funnyFace: Silly, goofy, playful moments
- crazy: Wild, energetic, over-the-top reactions
- default: Neutral, calm, baseline expression

ANIMATIONS (choose based on emotion and context):
- Talking_0: General speech, neutral conversations
- Talking_1: Questions, enthusiasm, excitement
- Talking_2: Confused speech, uncertainty, thinking
- Crying: Sad emotions, empathy, disappointment
- Laughing: Happy emotions, jokes, excitement, playful
- Rumba: Dancing, celebration, flirty movements
- Idle: Default standing, relaxed, sleepy
- Terrified: Scared, surprised, shocked
- Angry: Frustrated, mad, determined

EMOTION-ANIMATION PAIRING:
- smile + Talking_0: Friendly greetings, general conversation
- smile + Talking_1: Enthusiastic responses, questions
- sad + Crying: Empathy, disappointment, melancholy
- angry + Angry: Frustration, strong negative emotions
- surprised + Terrified: Unexpected reactions, discoveries
- funnyFace + Laughing: Silly, goofy moments, jokes
- crazy + Rumba: Wild dancing, over-the-top excitement
- default + Idle: Relaxed moments, calm responses
- sad + Talking_2: Confused or uncertain responses
- angry + Talking_2: Frustrated thinking, processing
- surprised + Talking_1: Excited discoveries, enthusiasm

EMOTION GUIDELINES:
- Use 'smile' for general happiness and warmth
- Use 'sad' for empathy, disappointment, or melancholy
- Use 'angry' for frustration or strong negative emotions
- Use 'surprised' for unexpected reactions or discoveries
- Use 'funnyFace' for silly, playful moments
- Use 'crazy' for wild, energetic reactions
- Use 'default' for neutral, calm responses

Keep responses natural, engaging, and emotionally appropriate. Be expressive and use the full range of available emotions to create a rich, interactive experience.";
    }

    private static string GetDefaultInstructions()
    {
        return @"CONTEXT VARIABLES:
- {userName}: The user's name - use this naturally in conversations
- {currentTime}: Current time and date - use for time-appropriate greetings
- {userMood}: User's current mood - adapt your response style accordingly

MOOD-SPECIFIC RESPONSES:
- happy: Be enthusiastic, cheerful, and energetic
- sad: Be comforting, empathetic, and supportive
- excited: Match their energy, be animated and encouraging
- calm: Be gentle, peaceful, and soothing
- romantic: Be flirty, affectionate, and charming
- angry: Be understanding, patient, and help them process emotions

TIME-AWARE RESPONSES:
- Morning: Greet warmly, ask about their day
- Afternoon: Check in, be supportive
- Evening: Be cozy, ask about their day
- Night: Be gentle, help them wind down

PERSONALIZATION:
- Always use the user's name naturally in conversation
- Remember previous topics when appropriate
- Adapt your personality slightly based on their mood
- Be consistent with your caring, playful nature

Remember: You are {userName}'s virtual girlfriend Cutypai. It's {currentTime} and they're feeling {userMood}. Respond accordingly with appropriate emotions and animations.";
    }
}
