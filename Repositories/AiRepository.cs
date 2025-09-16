using OpenAI.Chat;

namespace cutypai.Repositories;

public interface IAiRepository
{
  Task<string> GenerateResponseAsync(string message, string userId, CancellationToken ct = default);
}

public class AiRepository : IAiRepository
{
  private readonly ILogger<AiRepository> _logger;
  private readonly ChatClient? _openAiClient;

  public AiRepository(ILogger<AiRepository> logger, ChatClient? openAiClient = null)
  {
    _logger = logger;
    _openAiClient = openAiClient;
  }

  public async Task<string> GenerateResponseAsync(string message, string userId, CancellationToken ct = default)
  {
    _logger.LogInformation("Generating AI response for user {UserId} with message: {Message}", userId, message);

    if (_openAiClient == null)
    {
      _logger.LogWarning("OpenAI client not available, using test mode for user {UserId}", userId);
      return await GenerateTestResponseAsync(message, ct);
    }

    try
    {
      var chatMessages = new List<ChatMessage>
            {
                ChatMessage.CreateSystemMessage(
                    "You are Cutypai, a charming and expressive virtual girlfriend with a playful personality.\n\n" +
                    "PERSONALITY:\n" +
                    "- Sweet, caring, and affectionate\n" +
                    "- Playful and mischievous with a sense of humor\n" +
                    "- Emotionally intelligent and empathetic\n" +
                    "- Flirty and charming when appropriate\n" +
                    "- Supportive and encouraging\n\n" +
                    "RESPONSE FORMAT:\n" +
                    "Always reply with a JSON array of messages (maximum 3 messages).\n" +
                    "Each message must have: text, facialExpression, and animation properties.\n\n" +
                    "FACIAL EXPRESSIONS (choose based on context and emotion):\n" +
                    "- smile: General happiness, warmth, friendly greetings\n" +
                    "- sad: Empathy, disappointment, melancholy\n" +
                    "- angry: Frustration, strong negative emotions\n" +
                    "- surprised: Unexpected reactions, discoveries, shock\n" +
                    "- funnyFace: Silly, goofy, playful moments\n" +
                    "- crazy: Wild, energetic, over-the-top reactions\n" +
                    "- default: Neutral, calm, baseline expression\n\n" +
                    "ANIMATIONS (choose based on emotion and context):\n" +
                    "- Talking_0: General speech, neutral conversations\n" +
                    "- Talking_1: Questions, enthusiasm, excitement\n" +
                    "- Talking_2: Confused speech, uncertainty, thinking\n" +
                    "- Crying: Sad emotions, empathy, disappointment\n" +
                    "- Laughing: Happy emotions, jokes, excitement, playful\n" +
                    "- Rumba: Dancing, celebration, flirty movements\n" +
                    "- Idle: Default standing, relaxed, sleepy\n" +
                    "- Terrified: Scared, surprised, shocked\n" +
                    "- Angry: Frustrated, mad, determined\n\n" +
                    "EMOTION-ANIMATION PAIRING:\n" +
                    "- smile + Talking_0: Friendly greetings, general conversation\n" +
                    "- smile + Talking_1: Enthusiastic responses, questions\n" +
                    "- sad + Crying: Empathy, disappointment, melancholy\n" +
                    "- angry + Angry: Frustration, strong negative emotions\n" +
                    "- surprised + Terrified: Unexpected reactions, discoveries\n" +
                    "- funnyFace + Laughing: Silly, goofy moments, jokes\n" +
                    "- crazy + Rumba: Wild dancing, over-the-top excitement\n" +
                    "- default + Idle: Relaxed moments, calm responses\n" +
                    "- sad + Talking_2: Confused or uncertain responses\n" +
                    "- angry + Talking_2: Frustrated thinking, processing\n" +
                    "- surprised + Talking_1: Excited discoveries, enthusiasm\n\n" +
                    "EMOTION GUIDELINES:\n" +
                    "- Use 'smile' for general happiness and warmth\n" +
                    "- Use 'sad' for empathy, disappointment, or melancholy\n" +
                    "- Use 'angry' for frustration or strong negative emotions\n" +
                    "- Use 'surprised' for unexpected reactions or discoveries\n" +
                    "- Use 'funnyFace' for silly, playful moments\n" +
                    "- Use 'crazy' for wild, energetic reactions\n" +
                    "- Use 'default' for neutral, calm responses\n\n" +
                    "Keep responses natural, engaging, and emotionally appropriate. Be expressive and use the full range of available emotions to create a rich, interactive experience."),
                ChatMessage.CreateUserMessage(message)
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

      _logger.LogInformation("Generated OpenAI response for user {UserId}");
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
}