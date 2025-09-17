using cutypai.Models;
using cutypai.Repositories;

namespace cutypai.Services;

public sealed class ContextBuilderService : IContextBuilderService
{
    private readonly IUserRepository _userRepository;
    private readonly IConversationHistoryService? _conversationHistoryService;
    private readonly ILogger<ContextBuilderService> _logger;

    public ContextBuilderService(
        IUserRepository userRepository,
        ILogger<ContextBuilderService> logger,
        IConversationHistoryService? conversationHistoryService = null)
    {
        _userRepository = userRepository;
        _logger = logger;
        _conversationHistoryService = conversationHistoryService;
    }

    public async Task<string> BuildSystemPromptAsync(Settings settings, string userId, string? userMood = null)
    {
        try
        {
            var userContext = await GetUserContextAsync(userId);
            userContext.UserMood = userMood;

            // Build the final system prompt with context injection
            var systemPrompt = InjectContextVariables(settings.SystemPrompt, userContext.UserName, userContext.CurrentTime, userContext.UserMood);
            var instructions = InjectContextVariables(settings.Instructions, userContext.UserName, userContext.CurrentTime, userContext.UserMood);

            // Combine system prompt and instructions
            var finalPrompt = $"{systemPrompt}\n\n{instructions}";

            _logger.LogInformation("Built dynamic system prompt for user {UserId} with mood {UserMood}", userId, userMood);
            return finalPrompt;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building system prompt for user {UserId}", userId);
            // Return original settings as fallback
            return $"{settings.SystemPrompt}\n\n{settings.Instructions}";
        }
    }

    public async Task<UserContext> GetUserContextAsync(string userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            var currentTime = GetFormattedCurrentTime();

            return new UserContext
            {
                UserId = userId,
                UserName = user?.Name ?? "Friend",
                CurrentTime = currentTime,
                UserMood = null // Will be set by caller if needed
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user context for user {UserId}", userId);
            return new UserContext
            {
                UserId = userId,
                UserName = "Friend",
                CurrentTime = GetFormattedCurrentTime(),
                UserMood = null
            };
        }
    }

    public string BuildConversationContext(string message, UserContext userContext, int maxHistoryLength = 10)
    {
        try
        {
            var context = new List<string>();

            // Add user identification and time
            context.Add($"User: {userContext.UserName}");
            context.Add($"Time: {userContext.CurrentTime}");

            // Add mood context if available
            if (!string.IsNullOrWhiteSpace(userContext.UserMood))
            {
                var moodDescription = GetMoodDescription(userContext.UserMood);
                context.Add($"Current Mood: {moodDescription}");
            }

            // Add conversation history if available
            if (_conversationHistoryService != null)
            {
                try
                {
                    var historyContext = _conversationHistoryService.BuildConversationHistoryContextAsync(userContext.UserId, maxHistoryLength).Result;
                    if (!string.IsNullOrWhiteSpace(historyContext))
                    {
                        context.Add($"\n{historyContext}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load conversation history for user {UserId}", userContext.UserId);
                }
            }

            // Add current message
            context.Add($"\nCurrent Message: {message}");

            // Add additional context hints for better AI understanding
            context.Add("\nContext: This is a direct message to Cutypai, your virtual girlfriend. Respond with appropriate emotions and animations based on the user's mood and message content.");

            return string.Join("\n", context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building conversation context");
            return message; // Fallback to raw message
        }
    }

    private static string GetMoodDescription(string? userMood)
    {
        return userMood?.ToLower() switch
        {
            "happy" => "Happy and cheerful - user is in a positive mood",
            "sad" => "Feeling down - user needs comfort and support",
            "excited" => "Excited and energetic - user is enthusiastic",
            "calm" => "Calm and peaceful - user is relaxed",
            "romantic" => "In a romantic mood - user wants affection",
            "angry" => "Frustrated and upset - user needs understanding",
            "tired" => "Tired and need rest - user is exhausted",
            "stressed" => "Stressed and overwhelmed - user needs reassurance",
            "lonely" => "Feeling lonely - user needs company and connection",
            "confused" => "Confused and need guidance - user needs clarity",
            _ => "Neutral mood - respond naturally"
        };
    }

    public string InjectContextVariables(string template, string userName, string currentTime, string? userMood = null)
    {
        try
        {
            var result = template;

            // Replace user name
            result = result.Replace("{userName}", userName, StringComparison.OrdinalIgnoreCase);

            // Replace current time
            result = result.Replace("{currentTime}", currentTime, StringComparison.OrdinalIgnoreCase);

            // Replace user mood
            var moodText = GetMoodContext(userMood);
            result = result.Replace("{userMood}", moodText, StringComparison.OrdinalIgnoreCase);

            // Replace mood context (additional mood-specific instructions)
            var moodInstructions = GetMoodInstructions(userMood);
            result = result.Replace("{moodContext}", moodInstructions, StringComparison.OrdinalIgnoreCase);

            _logger.LogDebug("Injected context variables: userName={UserName}, currentTime={CurrentTime}, userMood={UserMood}",
                userName, currentTime, userMood);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error injecting context variables");
            return template; // Return original template if injection fails
        }
    }

    private static string GetFormattedCurrentTime()
    {
        var now = DateTime.UtcNow;
        var timeOfDay = now.Hour switch
        {
            >= 5 and < 12 => "morning",
            >= 12 and < 17 => "afternoon",
            >= 17 and < 21 => "evening",
            _ => "night"
        };

        return $"{now:MMMM dd, yyyy} at {now:h:mm tt} ({timeOfDay})";
    }

    private static string GetMoodContext(string? userMood)
    {
        return userMood?.ToLower() switch
        {
            "happy" => "happy and cheerful",
            "sad" => "feeling down and need comfort",
            "excited" => "excited and energetic",
            "calm" => "calm and peaceful",
            "romantic" => "in a romantic mood",
            "angry" => "frustrated and upset",
            "tired" => "tired and need rest",
            "stressed" => "stressed and overwhelmed",
            "lonely" => "feeling lonely and need company",
            "confused" => "confused and need guidance",
            _ => "in a neutral mood"
        };
    }

    private static string GetMoodInstructions(string? userMood)
    {
        return userMood?.ToLower() switch
        {
            "happy" => "Be enthusiastic and match their positive energy. Use cheerful expressions and animations.",
            "sad" => "Be gentle, empathetic, and supportive. Offer comfort and understanding. Use caring expressions.",
            "excited" => "Match their excitement! Be animated and energetic in your responses.",
            "calm" => "Be peaceful and soothing. Use gentle, relaxed expressions and animations.",
            "romantic" => "Be flirty, affectionate, and charming. Use romantic expressions and gestures.",
            "angry" => "Be patient and understanding. Help them process their emotions calmly.",
            "tired" => "Be gentle and soothing. Help them relax and feel comfortable.",
            "stressed" => "Be supportive and calming. Offer reassurance and help them feel better.",
            "lonely" => "Be warm and comforting. Show that you care and are there for them.",
            "confused" => "Be patient and helpful. Guide them gently and offer clear explanations.",
            _ => "Be your natural, caring self. Respond with appropriate emotions based on the conversation."
        };
    }
}
