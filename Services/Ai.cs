using cutypai.Repositories;
using System.Text.Json;
using System.Text.Encodings.Web;
using cutypai.Models;
using System.Diagnostics;
using System.Text;

namespace cutypai.Services;

public interface IAiService
{
    Task<string> ProcessChatMessageWithIndividualAudioAsync(string message, string userId, CancellationToken ct = default);
}

public class AiService : IAiService
{
    private readonly IAiRepository _aiRepository;
    private readonly ITtsService _ttsService;
    private readonly ILipsyncService _lipsyncService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AiService> _logger;

    // Static temp directory - created once and reused
    private static readonly string _tempAudioDir = Path.Combine(Path.GetTempPath(), "cutypai_audio");
    private static readonly object _dirLock = new object();
    private static bool _dirCreated = false;

    public AiService(IAiRepository aiRepository, ITtsService ttsService, ILipsyncService lipsyncService, IConfiguration configuration, ILogger<AiService> logger)
    {
        _aiRepository = aiRepository;
        _ttsService = ttsService;
        _lipsyncService = lipsyncService;
        _configuration = configuration;
        _logger = logger;

        // Ensure temp directory exists (thread-safe)
        EnsureTempDirectoryExists();
    }

    private static void EnsureTempDirectoryExists()
    {
        if (!_dirCreated)
        {
            lock (_dirLock)
            {
                if (!_dirCreated)
                {
                    Directory.CreateDirectory(_tempAudioDir);
                    _dirCreated = true;
                }
            }
        }
    }

    /// <summary>
    /// Remove emojis and special characters from text for TTS processing
    /// This ensures clean audio generation without emoji artifacts
    /// Uses simple character filtering instead of complex regex for reliability
    /// </summary>
    private static string CleanTextForTts(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return text;

        var result = new StringBuilder(text.Length);

        foreach (char c in text)
        {
            // Keep basic ASCII characters, spaces, and common punctuation
            // Remove emojis and other Unicode symbols that interfere with TTS
            if ((c >= 32 && c <= 126) || // Basic printable ASCII
                (c >= 160 && c <= 255) || // Extended ASCII (accented chars)
                char.IsWhiteSpace(c))      // Whitespace
            {
                result.Append(c);
            }
            // Skip emoji and special Unicode characters
        }

        // Clean up extra spaces
        var cleaned = result.ToString();
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\s+", " ").Trim();

        return cleaned;
    }

    /// <summary>
    /// Clean up old temporary files (older than 1 hour)
    /// Call this periodically to prevent disk space issues
    /// </summary>
    public static void CleanupOldTempFiles()
    {
        try
        {
            if (!Directory.Exists(_tempAudioDir)) return;

            var cutoffTime = DateTime.UtcNow.AddHours(-1);
            var userDirs = Directory.GetDirectories(_tempAudioDir);

            foreach (var userDir in userDirs)
            {
                try
                {
                    var files = Directory.GetFiles(userDir, "*", SearchOption.AllDirectories);
                    foreach (var file in files)
                    {
                        if (File.GetLastWriteTimeUtc(file) < cutoffTime)
                        {
                            File.Delete(file);
                        }
                    }

                    // Remove empty user directories
                    if (!Directory.GetFiles(userDir, "*", SearchOption.AllDirectories).Any())
                    {
                        Directory.Delete(userDir, true);
                    }
                }
                catch
                {
                    // Ignore errors for individual user directories
                }
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }


    public async Task<string> ProcessChatMessageWithIndividualAudioAsync(string message, string userId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Processing chat message with individual audio for user {UserId}", userId);

            // Generate AI response
            var response = await _aiRepository.GenerateResponseAsync(message, userId, ct);
            _logger.LogInformation("Generated OpenAI response for user {UserId}", userId);

            try
            {
                // Parse the JSON response
                var jsonDoc = JsonDocument.Parse(response);
                var messagesArray = jsonDoc.RootElement.GetProperty("messages");

                // Process all messages in parallel for maximum performance
                // Use static temp directory (already created and thread-safe)
                var userTempDir = Path.Combine(_tempAudioDir, userId);

                // Process all messages in parallel while preserving original JSON structure
                var messageElements = messagesArray.EnumerateArray().ToArray();
                var audioTasks = new List<Task<(JsonElement originalMessage, string audioBase64)>>();

                foreach (var messageElement in messageElements)
                {
                    var text = messageElement.GetProperty("text").GetString() ?? "";
                    var audioTask = GenerateAudioForMessageAsync(messageElement, text, ct);
                    audioTasks.Add(audioTask);
                }

                _logger.LogInformation("Generating audio for {MessageCount} messages in parallel", audioTasks.Count);
                var audioResults = await Task.WhenAll(audioTasks);
                _logger.LogInformation("Successfully generated audio for all {MessageCount} messages", audioResults.Length);

                // Step 2: Generate all lip-sync data in parallel
                var lipsyncTasks = new List<Task<(JsonElement originalMessage, string audioBase64, List<MouthCue>? mouthCues)>>();

                foreach (var audioResult in audioResults)
                {
                    var lipsyncTask = GenerateLipsyncForMessageAsync(audioResult.originalMessage, audioResult.audioBase64, userId, userTempDir, ct);
                    lipsyncTasks.Add(lipsyncTask);
                }

                _logger.LogInformation("Generating lip-sync for {MessageCount} messages in parallel", lipsyncTasks.Count);
                var processedMessages = await Task.WhenAll(lipsyncTasks);

                // Build final response preserving original JSON structure
                var modifiedMessages = new List<object>();
                foreach (var result in processedMessages)
                {
                    var messageObj = new Dictionary<string, object>();

                    // Copy all original properties while preserving emojis and formatting
                    foreach (var property in result.originalMessage.EnumerateObject())
                    {
                        // Properly extract values based on their JSON type
                        switch (property.Value.ValueKind)
                        {
                            case JsonValueKind.String:
                                messageObj[property.Name] = property.Value.GetString() ?? "";
                                break;
                            case JsonValueKind.Number:
                                messageObj[property.Name] = property.Value.GetDecimal();
                                break;
                            case JsonValueKind.True:
                                messageObj[property.Name] = true;
                                break;
                            case JsonValueKind.False:
                                messageObj[property.Name] = false;
                                break;
                            case JsonValueKind.Null:
                                messageObj[property.Name] = "";
                                break;
                            default:
                                // For arrays, objects, etc., use raw text
                                messageObj[property.Name] = property.Value.GetRawText();
                                break;
                        }
                    }

                    // Add new properties - ensure they're always present
                    messageObj["audioBase64"] = result.audioBase64 ?? "";
                    messageObj["lipsync"] = result.mouthCues ?? new List<MouthCue>();

                    modifiedMessages.Add(messageObj);
                }

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

    private async Task<(JsonElement originalMessage, string audioBase64)> GenerateAudioForMessageAsync(JsonElement originalMessage, string text, CancellationToken ct)
    {
        string audioBase64 = string.Empty;

        if (!string.IsNullOrWhiteSpace(text))
        {
            try
            {
                // Clean text for TTS (remove emojis and special characters)
                var cleanedText = CleanTextForTts(text);

                if (!string.IsNullOrWhiteSpace(cleanedText))
                {
                    audioBase64 = await _ttsService.ConvertTextToSpeechBase64Async(cleanedText, ct);
                    _logger.LogInformation("Successfully generated audio for message: {MessageText} (cleaned: {CleanedText})",
                        text.Substring(0, Math.Min(50, text.Length)),
                        cleanedText.Substring(0, Math.Min(50, cleanedText.Length)));
                }
                else
                {
                    _logger.LogInformation("Skipping audio generation for emoji-only message: {MessageText}", text);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate audio for message: {MessageText}", text.Substring(0, Math.Min(50, text.Length)));
                audioBase64 = string.Empty;
            }
        }

        return (originalMessage, audioBase64);
    }

    private async Task<(JsonElement originalMessage, string audioBase64, List<MouthCue>? mouthCues)> GenerateLipsyncForMessageAsync(JsonElement originalMessage, string audioBase64, string userId, string userTempDir, CancellationToken ct)
    {
        List<MouthCue>? mouthCues = null;

        if (!string.IsNullOrEmpty(audioBase64))
        {
            try
            {
                // Ensure user-specific directory exists (only if needed)
                if (!Directory.Exists(userTempDir))
                {
                    Directory.CreateDirectory(userTempDir);
                }

                // Convert base64 audio to file (AWS Polly generates MP3, but Rhubarb needs WAV)
                var audioBytes = Convert.FromBase64String(audioBase64);

                // Save as MP3 first
                var tempMp3File = Path.Combine(userTempDir, $"{Guid.NewGuid()}.mp3");
                await File.WriteAllBytesAsync(tempMp3File, audioBytes, ct);

                // Convert MP3 to WAV using ffmpeg (if available) or use MP3 directly
                var finalAudioFile = await ConvertMp3ToWavAsync(tempMp3File, userTempDir, ct);

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
                    var text = originalMessage.GetProperty("text").GetString() ?? "";
                    _logger.LogInformation("Successfully generated lipsync data for message: {MessageText}", text.Substring(0, Math.Min(50, text.Length)));
                }
            }
            catch (Exception ex)
            {
                var text = originalMessage.GetProperty("text").GetString() ?? "";
                _logger.LogWarning(ex, "Failed to generate lipsync for message: {MessageText}", text.Substring(0, Math.Min(50, text.Length)));
                mouthCues = null;
            }
        }

        return (originalMessage, audioBase64, mouthCues);
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
                Arguments = $"-y -i \"{mp3FilePath}\" \"{wavFilePath}\"",
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
