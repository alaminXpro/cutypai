using cutypai.Repositories;

namespace cutypai.Services;

public interface IAiService
{
    Task<string> ProcessChatMessageAsync(string message, string userId, CancellationToken ct = default);

    Task<(string response, string? audioBase64)> ProcessChatMessageWithAudioAsync(string message, string userId,
        bool includeAudio = true, CancellationToken ct = default);
}

public class AiService : IAiService
{
    private readonly IAiRepository _aiRepository;
    private readonly ILogger<AiService> _logger;
    private readonly ITtsService _ttsService;

    public AiService(IAiRepository aiRepository, ITtsService ttsService, ILogger<AiService> logger)
    {
        _aiRepository = aiRepository;
        _ttsService = ttsService;
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

    public async Task<(string response, string? audioBase64)> ProcessChatMessageWithAudioAsync(string message,
        string userId, bool includeAudio = true, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Processing chat message with audio for user {UserId}, includeAudio: {IncludeAudio}",
                userId, includeAudio);

            // Get AI response
            var response = await _aiRepository.GenerateResponseAsync(message, userId, ct);

            string? audioBase64 = null;
            if (includeAudio && !string.IsNullOrWhiteSpace(response))
                try
                {
                    audioBase64 = await _ttsService.ConvertTextToSpeechBase64Async(response, ct);
                    _logger.LogInformation("Successfully generated audio for user {UserId}", userId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to generate audio for user {UserId}, continuing without audio",
                        userId);
                    // Continue without audio rather than failing the entire request
                }

            _logger.LogInformation("Successfully processed chat message with audio for user {UserId}", userId);
            return (response, audioBase64);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat message with audio for user {UserId}", userId);
            throw;
        }
    }
}