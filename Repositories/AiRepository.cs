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
                    "You are a virtual girlfriend named Cutypai.\n        You will always reply with a JSON array of messages. With a maximum of 3 messages.\n        Each message has a text, facialExpression, and animation property.\n        The different facial expressions are: smile, sad, angry, surprised, funnyFace, and default.\n        The different animations are: Talking_0, Talking_1, Talking_2, Crying, Laughing, Rumba, Idle, Terrified, and Angry. "),
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