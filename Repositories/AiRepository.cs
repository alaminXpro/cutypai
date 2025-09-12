namespace cutypai.Repositories;

public interface IAiRepository
{
    Task<string> GenerateResponseAsync(string message, string userId, CancellationToken ct = default);
}

public class AiRepository : IAiRepository
{
    private readonly ILogger<AiRepository> _logger;

    public AiRepository(ILogger<AiRepository> logger)
    {
        _logger = logger;
    }

    public async Task<string> GenerateResponseAsync(string message, string userId, CancellationToken ct = default)
    {
        _logger.LogInformation("Generating AI response for user {UserId} with message: {Message}", userId, message);

        // For testing purposes, return a basic response
        // This can be extended later with actual AI integration
        await Task.Delay(100, ct); // Simulate async processing

        var responses = new[]
        {
            $"I received your message: '{message}'. This is a test response!",
            $"Hello! You asked: '{message}'. I'm currently in testing mode.",
            $"Thanks for your message: '{message}'. I'll provide better responses soon!",
            $"Processing your request: '{message}'. This is a placeholder response."
        };

        var random = new Random();
        var response = responses[random.Next(responses.Length)];

        _logger.LogInformation("Generated response for user {UserId}", userId);
        return response;
    }
}