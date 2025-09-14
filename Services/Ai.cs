using cutypai.Repositories;
using System.Text.Json;
using System.Text.Encodings.Web;
using cutypai.Models;
using System.Diagnostics;

namespace cutypai.Services;

public interface IAiService
{
    Task<string> ProcessChatMessageAsync(string message, string userId, CancellationToken ct = default);
    Task<string> ProcessChatMessageWithAudioAsync(string message, string userId, CancellationToken ct = default);
    Task<string> ProcessChatMessageWithIndividualAudioAsync(string message, string userId, bool includeAudio = true, CancellationToken ct = default);
}

public class AiService : IAiService
{
    private readonly IAiRepository _aiRepository;
    private readonly ITtsService _ttsService;
    private readonly ILipsyncService _lipsyncService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AiService> _logger;

    public AiService(IAiRepository aiRepository, ITtsService ttsService, ILipsyncService lipsyncService, IConfiguration configuration, ILogger<AiService> logger)
    {
        _aiRepository = aiRepository;
        _ttsService = ttsService;
        _lipsyncService = lipsyncService;
        _configuration = configuration;
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

    public async Task<string> ProcessChatMessageWithAudioAsync(string message, string userId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Processing chat message with audio for user {UserId}", userId);
            var response = await _aiRepository.GenerateResponseAsync(message, userId, ct);
            Console.WriteLine(response);

            // Parse the JSON response
            var jsonDoc = JsonDocument.Parse(response);
            var messagesArray = jsonDoc.RootElement.GetProperty("messages");

            // Generate audio for the first message only
            if (messagesArray.GetArrayLength() > 0)
            {
                var firstMessage = messagesArray[0];
                var text = firstMessage.GetProperty("text").GetString() ?? "";

                if (!string.IsNullOrWhiteSpace(text))
                {
                    try
                    {
                        var audioBase64 = await _ttsService.ConvertTextToSpeechBase64Async(text, ct);
                        _logger.LogInformation("Successfully generated audio for message: {MessageText}", text.Substring(0, Math.Min(50, text.Length)));

                        // Create a new response with audio
                        var responseWithAudio = new
                        {
                            messages = new[]
                            {
                                new
                                {
                                    text = text,
                                    facialExpression = firstMessage.GetProperty("facialExpression").GetString() ?? "",
                                    animation = firstMessage.GetProperty("animation").GetString() ?? "",
                                    audioBase64 = audioBase64
                                }
                            }
                        };

                        return JsonSerializer.Serialize(responseWithAudio, new JsonSerializerOptions
                        {
                            WriteIndented = true,
                            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to generate audio for message: {MessageText}", text.Substring(0, Math.Min(50, text.Length)));
                    }
                }
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat message with audio for user {UserId}", userId);
            throw;
        }
    }

    public async Task<string> ProcessChatMessageWithIndividualAudioAsync(string message, string userId, bool includeAudio = true, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Processing chat message with individual audio for user {UserId}, includeAudio: {IncludeAudio}", userId, includeAudio);

            // Generate AI response
            var response = await _aiRepository.GenerateResponseAsync(message, userId, ct);
            _logger.LogInformation("Generated OpenAI response for user {UserId}", userId);

            try
            {
                // Parse the JSON response
                var jsonDoc = JsonDocument.Parse(response);
                var messagesArray = jsonDoc.RootElement.GetProperty("messages");

                // Process all messages in parallel for maximum performance
                var tempAudioDir = Path.Combine(Path.GetTempPath(), "cutypai_audio", userId);
                Directory.CreateDirectory(tempAudioDir);

                // Step 1: Generate all audio in parallel
                var audioTasks = new List<Task<(string text, string facialExpression, string animation, string audioBase64)>>();

                foreach (var messageElement in messagesArray.EnumerateArray())
                {
                    var text = messageElement.GetProperty("text").GetString() ?? "";
                    var facialExpression = messageElement.GetProperty("facialExpression").GetString() ?? "";
                    var animation = messageElement.GetProperty("animation").GetString() ?? "";

                    var audioTask = GenerateAudioAsync(text, facialExpression, animation, includeAudio, ct);
                    audioTasks.Add(audioTask);
                }

                _logger.LogInformation("Generating audio for {MessageCount} messages in parallel", audioTasks.Count);
                var audioResults = await Task.WhenAll(audioTasks);
                _logger.LogInformation("Successfully generated audio for all {MessageCount} messages", audioResults.Length);

                // Step 2: Generate all lip-sync data in parallel
                var lipsyncTasks = new List<Task<(string text, string facialExpression, string animation, string audioBase64, List<MouthCue>? mouthCues)>>();

                foreach (var audioResult in audioResults)
                {
                    var lipsyncTask = GenerateLipsyncAsync(audioResult.text, audioResult.facialExpression, audioResult.animation, audioResult.audioBase64, userId, tempAudioDir, ct);
                    lipsyncTasks.Add(lipsyncTask);
                }

                _logger.LogInformation("Generating lip-sync for {MessageCount} messages in parallel", lipsyncTasks.Count);
                var processedMessages = await Task.WhenAll(lipsyncTasks);
                var modifiedMessages = processedMessages.Select(result => new
                {
                    text = result.text,
                    facialExpression = result.facialExpression,
                    animation = result.animation,
                    audioBase64 = result.audioBase64,
                    lipsync = result.mouthCues
                }).ToList();

                _logger.LogInformation("Successfully processed all {MessageCount} messages in parallel", processedMessages.Length);

                // Create the final response
                var finalResponse = new
                {
                    messages = modifiedMessages
                };

                var jsonResponse = JsonSerializer.Serialize(finalResponse, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                _logger.LogInformation("Successfully processed chat message with individual audio for user {UserId}", userId);
                return jsonResponse;
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse JSON response for user {UserId}, returning original response", userId);
                return response;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat message with individual audio for user {UserId}", userId);
            throw;
        }
    }

    private async Task<(string text, string facialExpression, string animation, string audioBase64)> GenerateAudioAsync(string text, string facialExpression, string animation, bool includeAudio, CancellationToken ct)
    {
        string audioBase64 = string.Empty;

        if (includeAudio && !string.IsNullOrWhiteSpace(text))
        {
            try
            {
                audioBase64 = await _ttsService.ConvertTextToSpeechBase64Async(text, ct);
                _logger.LogInformation("Successfully generated audio for message: {MessageText}", text.Substring(0, Math.Min(50, text.Length)));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate audio for message: {MessageText}", text.Substring(0, Math.Min(50, text.Length)));
                audioBase64 = string.Empty;
            }
        }

        return (text, facialExpression, animation, audioBase64);
    }

    private async Task<(string text, string facialExpression, string animation, string audioBase64, List<MouthCue>? mouthCues)> GenerateLipsyncAsync(string text, string facialExpression, string animation, string audioBase64, string userId, string tempAudioDir, CancellationToken ct)
    {
        List<MouthCue>? mouthCues = null;

        if (!string.IsNullOrEmpty(audioBase64))
        {
            try
            {
                // Convert base64 audio to file (AWS Polly generates MP3, but Rhubarb needs WAV)
                var audioBytes = Convert.FromBase64String(audioBase64);

                // Save as MP3 first
                var tempMp3File = Path.Combine(tempAudioDir, $"{Guid.NewGuid()}.mp3");
                await File.WriteAllBytesAsync(tempMp3File, audioBytes, ct);

                // Convert MP3 to WAV using ffmpeg (if available) or use MP3 directly
                var finalAudioFile = await ConvertMp3ToWavAsync(tempMp3File, tempAudioDir, ct);

                // Generate lipsync data
                var lipsyncData = await _lipsyncService.GenerateLipsyncDataAsync(finalAudioFile, ct);

                // Clean up temporary file
                try
                {
                    File.Delete(finalAudioFile);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete temporary audio file: {TempFile}", finalAudioFile);
                }

                if (lipsyncData != null)
                {
                    mouthCues = lipsyncData.MouthCues;
                    _logger.LogInformation("Successfully generated lipsync data for message: {MessageText}", text.Substring(0, Math.Min(50, text.Length)));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate lipsync for message: {MessageText}", text.Substring(0, Math.Min(50, text.Length)));
                mouthCues = null;
            }
        }

        return (text, facialExpression, animation, audioBase64, mouthCues);
    }


    private async Task<string> ConvertMp3ToWavAsync(string mp3FilePath, string outputDir, CancellationToken ct)
    {
        try
        {
            // Check if ffmpeg is available
            var ffmpegPath = await FindFfmpegAsync();
            if (string.IsNullOrEmpty(ffmpegPath))
            {
                _logger.LogWarning("ffmpeg not found, using MP3 file directly (may not work with Rhubarb)");
                return mp3FilePath;
            }

            var wavFilePath = Path.Combine(outputDir, $"{Guid.NewGuid()}.wav");

            var processStartInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = $"-i \"{mp3FilePath}\" -acodec pcm_s16le -ar 22050 \"{wavFilePath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processStartInfo };
            process.Start();

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode == 0 && File.Exists(wavFilePath))
            {
                _logger.LogInformation("Successfully converted MP3 to WAV: {WavFile}", wavFilePath);
                return wavFilePath;
            }
            else
            {
                _logger.LogWarning("ffmpeg conversion failed, using MP3 file directly. Error: {Error}", error);
                return mp3FilePath;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error converting MP3 to WAV, using MP3 file directly");
            return mp3FilePath;
        }
    }

    private async Task<string?> FindFfmpegAsync()
    {
        try
        {
            // Check common locations for ffmpeg
            var possiblePaths = new[]
            {
                "ffmpeg",
                "/usr/local/bin/ffmpeg",
                "/opt/homebrew/bin/ffmpeg",
                "/usr/bin/ffmpeg"
            };

            foreach (var path in possiblePaths)
            {
                try
                {
                    var processStartInfo = new ProcessStartInfo
                    {
                        FileName = path,
                        Arguments = "-version",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using var process = new Process { StartInfo = processStartInfo };
                    process.Start();
                    await process.WaitForExitAsync();

                    if (process.ExitCode == 0)
                    {
                        return path;
                    }
                }
                catch
                {
                    // Continue to next path
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}
