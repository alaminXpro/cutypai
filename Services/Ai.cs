using cutypai.Repositories;

namespace cutypai.Services;

public interface IAiService
{
    Task<string> ProcessChatMessageAsync(string message, string userId, CancellationToken ct = default);
}

public class AiService : IAiService
{
    private readonly IAiRepository _aiRepository;
    private readonly ILogger<AiService> _logger;

    public AiService(IAiRepository aiRepository, ILogger<AiService> logger)
    {
        _aiRepository = aiRepository;
        _logger = logger;
    }

    public async Task<string> ProcessChatMessageAsync(string message, string userId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Processing chat message for user {UserId}", userId);

            var response = await _aiRepository.GenerateResponseAsync(message, userId, ct);

            _logger.LogInformation("Successfully processed chat message for user {UserId}", userId);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat message for user {UserId}", userId);
            throw;
        }
    }
}