using Amazon.Polly;
using Amazon.Polly.Model;

namespace cutypai.Services;

public class PollyTtsService : ITtsService
{
    private readonly ILogger<PollyTtsService> _logger;
    private readonly IAmazonPolly _pollyClient;

    public PollyTtsService(IAmazonPolly pollyClient, ILogger<PollyTtsService> logger)
    {
        _pollyClient = pollyClient;
        _logger = logger;
    }

    public async Task<byte[]> ConvertTextToSpeechAsync(string text, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Converting text to speech, length: {TextLength}", text.Length);

            // Limit text length to avoid AWS limits (3000 characters for standard voices)
            if (text.Length > 2500) text = text.Substring(0, 2497) + "...";

            var request = new SynthesizeSpeechRequest
            {
                Text = text,
                OutputFormat = OutputFormat.Mp3,
                VoiceId = VoiceId.Kajal, // Female Hindi voice from India
                Engine = Engine.Neural, // Use neural engine for better quality
                TextType = TextType.Text
            };

            var response = await _pollyClient.SynthesizeSpeechAsync(request, ct);

            using var memoryStream = new MemoryStream();
            await response.AudioStream.CopyToAsync(memoryStream, ct);
            var audioBytes = memoryStream.ToArray();

            _logger.LogInformation("Successfully converted text to speech, audio size: {AudioSize} bytes",
                audioBytes.Length);
            return audioBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting text to speech");
            throw;
        }
    }

    public async Task<string> ConvertTextToSpeechBase64Async(string text, CancellationToken ct = default)
    {
        var audioBytes = await ConvertTextToSpeechAsync(text, ct);
        return Convert.ToBase64String(audioBytes);
    }
}