using OpenAI.Chat;
using cutypai.Models;
using cutypai.Services;

namespace cutypai.Repositories;

public interface IAiRepository
{
  Task<string> GenerateResponseAsync(string message, string userId, string? userMood = null, CancellationToken ct = default);
}

public class AiRepository : IAiRepository
{
  private readonly ILogger<AiRepository> _logger;
  private readonly ChatClient? _openAiClient;
  private readonly ISettingsRepository? _settingsRepository;
  private readonly IContextBuilderService? _contextBuilderService;

  public AiRepository(
    ILogger<AiRepository> logger,
    ChatClient? openAiClient = null,
    ISettingsRepository? settingsRepository = null,
    IContextBuilderService? contextBuilderService = null)
  {
    _logger = logger;
    _openAiClient = openAiClient;
    _settingsRepository = settingsRepository;
    _contextBuilderService = contextBuilderService;
  }

  public async Task<string> GenerateResponseAsync(string message, string userId, string? userMood = null, CancellationToken ct = default)
  {
    _logger.LogInformation("Generating AI response for user {UserId} with message: {Message} and mood: {UserMood}", userId, message, userMood);

    if (_openAiClient == null)
    {
      _logger.LogWarning("OpenAI client not available, using test mode for user {UserId}", userId);
      return await GenerateTestResponseAsync(message, ct);
    }

    try
    {
      // Get dynamic system prompt from database
      string systemPrompt;
      if (_settingsRepository != null && _contextBuilderService != null)
      {
        try
        {
          var settings = await _settingsRepository.GetActiveSettingsAsync(ct);
          if (settings != null)
          {
            systemPrompt = await _contextBuilderService.BuildSystemPromptAsync(settings, userId, userMood);
            _logger.LogInformation("Using dynamic system prompt for user {UserId}", userId);
          }
          else
          {
            systemPrompt = GetFallbackSystemPrompt();
            _logger.LogWarning("No settings found, using fallback system prompt for user {UserId}", userId);
          }
        }
        catch (Exception ex)
        {
          _logger.LogError(ex, "Error getting dynamic system prompt for user {UserId}, using fallback", userId);
          systemPrompt = GetFallbackSystemPrompt();
        }
      }
      else
      {
        systemPrompt = GetFallbackSystemPrompt();
        _logger.LogWarning("Settings services not available, using fallback system prompt for user {UserId}", userId);
      }

      // Build enriched user message with context
      var enrichedUserMessage = await BuildEnrichedUserMessageAsync(message, userId, userMood, ct);

      var chatMessages = new List<ChatMessage>
            {
                ChatMessage.CreateSystemMessage(systemPrompt),
                ChatMessage.CreateUserMessage(enrichedUserMessage)
            };
      ChatCompletionOptions options = new()
      {
        ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
              "cutypai_messages",
              BinaryData.FromBytes("""
                                         {
                                          "type": "object",
                                          "additionalProperties": false,
                                         "properties": {
                                           "messages": {
                                             "type": "array",
                                             "maxItems": 3,
                                             "items": {
                                               "type": "object",
                                               "additionalProperties": false,
                                               "properties": {
                                                 "text": {
                                                   "type": "string"
                                                 },
                                                 "facialExpression": {
                                                   "type": "string",
                                                   "enum": [
                                                     "smile",
                                                     "sad",
                                                     "angry",
                                                     "surprised",
                                                     "funnyFace",
                                                     "crazy",
                                                     "default"
                                                   ]
                                                 },
                                                 "animation": {
                                                   "type": "string",
                                                   "enum": [
                                                     "Talking_0",
                                                     "Talking_1", 
                                                     "Talking_2",
                                                     "Crying",
                                                     "Laughing",
                                                     "Rumba",
                                                     "Idle",
                                                     "Terrified",
                                                     "Angry"
                                                   ]
                                                 }
                                               },
                                               "required": [
                                                 "text",
                                                 "facialExpression",
                                                 "animation"
                                               ]
                                             }
                                           }
                                         },
                                         "required": [
                                           "messages"
                                         ]
                                         }
                                         """u8.ToArray()),
              jsonSchemaIsStrict: true),

        MaxOutputTokenCount = 1000,
        Temperature = (float)0.6
      };


      ChatCompletion completion = await _openAiClient.CompleteChatAsync(chatMessages, options);

      _logger.LogInformation("Generated OpenAI response for user {UserId}", userId);
      return completion.Content[0].Text;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error generating OpenAI response for user {UserId}, falling back to test mode",
          userId);
      return await GenerateTestResponseAsync(message, ct);
    }
  }

  private async Task<string> GenerateTestResponseAsync(string message, CancellationToken ct)
  {
    // Fallback test responses when OpenAI is not available
    await Task.Delay(100, ct); // Simulate async processing

    var responses = new[]
    {
            $"I received your message: '{message}'. This is a test response!",
            $"Hello! You asked: '{message}'. I'm currently in testing mode.",
            $"Thanks for your message: '{message}'. I'll provide better responses soon!",
            $"Processing your request: '{message}'. This is a placeholder response."
        };

    var random = new Random();
    return responses[random.Next(responses.Length)];
  }

  private async Task<string> BuildEnrichedUserMessageAsync(string message, string userId, string? userMood, CancellationToken ct)
  {
    try
    {
      if (_contextBuilderService == null)
      {
        _logger.LogWarning("Context builder service not available, using raw message for user {UserId}", userId);
        return message;
      }

      var userContext = await _contextBuilderService.GetUserContextAsync(userId);
      userContext.UserMood = userMood;

      // Build enriched context for the user message using the context builder service
      var enrichedMessage = _contextBuilderService.BuildConversationContext(message, userContext);

      _logger.LogInformation("Built enriched user message for user {UserId} with mood {UserMood}", userId, userMood);
      return enrichedMessage;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error building enriched user message for user {UserId}, using raw message", userId);
      return message; // Fallback to raw message
    }
  }

  private static string GetFallbackSystemPrompt()
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
}