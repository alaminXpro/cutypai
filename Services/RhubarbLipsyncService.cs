using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using cutypai.Models;

namespace cutypai.Services;

public class RhubarbLipsyncService : ILipsyncService
{
    private readonly ILogger<RhubarbLipsyncService> _logger;
    private readonly string _rhubarbPath;

    public RhubarbLipsyncService(ILogger<RhubarbLipsyncService> logger, IConfiguration configuration)
    {
        _logger = logger;

        // Get the configured path or use platform detection
        var configuredPath = configuration["RhubarbPath"];
        if (!string.IsNullOrEmpty(configuredPath))
        {
            _rhubarbPath = configuredPath;
        }
        else
        {
            // Platform detection fallback using RuntimeInformation
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                _rhubarbPath = "./bin/rhubarb-macos";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                _rhubarbPath = "./bin/rhubarb-linux";
            }
            else
            {
                // Windows or other platforms - default to Linux for production
                _rhubarbPath = "./bin/rhubarb-linux";
            }
        }

        _logger.LogInformation("Using Rhubarb binary: {RhubarbPath}", _rhubarbPath);
    }

    public async Task<LipsyncData?> GenerateLipsyncDataAsync(string audioFilePath, CancellationToken ct = default)
    {
        try
        {
            if (!File.Exists(audioFilePath))
            {
                _logger.LogWarning("Audio file not found: {AudioFilePath}", audioFilePath);
                return null;
            }

            if (!File.Exists(_rhubarbPath))
            {
                _logger.LogError("Rhubarb binary not found at: {RhubarbPath}. Please ensure the binary is in the correct location.", _rhubarbPath);
                return null;
            }

            _logger.LogInformation("Generating lipsync data for audio file: {AudioFilePath}", audioFilePath);

            var processStartInfo = new ProcessStartInfo
            {
                FileName = _rhubarbPath,
                Arguments = $"-f json -o \"{audioFilePath}.json\" \"{audioFilePath}\" -r phonetic --machineReadable",
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

            if (process.ExitCode != 0)
            {
                _logger.LogError("Rhubarb process failed with exit code {ExitCode}. Error: {Error}. Output: {Output}",
                    process.ExitCode, error, output);
                return null;
            }

            _logger.LogInformation("Rhubarb process completed successfully. Output: {Output}", output);

            // Read the JSON file that was generated
            var jsonFilePath = $"{audioFilePath}.json";
            if (File.Exists(jsonFilePath))
            {
                var jsonContent = await File.ReadAllTextAsync(jsonFilePath, ct);
                _logger.LogDebug("Rhubarb JSON content for {AudioFilePath}: {JsonContent}", audioFilePath, jsonContent);

                var lipsyncData = await ParseRhubarbJsonAsync(jsonContent, ct);

                // Clean up the JSON file
                try
                {
                    File.Delete(jsonFilePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete temporary JSON file: {JsonFile}", jsonFilePath);
                }

                if (lipsyncData != null)
                {
                    _logger.LogInformation("Successfully generated lipsync data for {AudioFilePath} with {CueCount} mouth cues",
                        audioFilePath, lipsyncData.MouthCues.Count);
                }
                else
                {
                    _logger.LogWarning("Failed to parse lipsync data for {AudioFilePath}", audioFilePath);
                }

                return lipsyncData;
            }
            else
            {
                _logger.LogError("Rhubarb JSON output file not found: {JsonFile}", jsonFilePath);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating lipsync data for {AudioFilePath}", audioFilePath);
            return null;
        }
    }


    private Task<LipsyncData?> ParseRhubarbJsonAsync(string jsonContent, CancellationToken ct)
    {
        try
        {
            using var jsonDoc = JsonDocument.Parse(jsonContent);
            var root = jsonDoc.RootElement;

            var mouthCues = new List<MouthCue>();

            if (root.TryGetProperty("mouthCues", out var mouthCuesArray))
            {
                foreach (var cue in mouthCuesArray.EnumerateArray())
                {
                    if (cue.TryGetProperty("start", out var startElement) &&
                        cue.TryGetProperty("end", out var endElement) &&
                        cue.TryGetProperty("value", out var valueElement))
                    {
                        mouthCues.Add(new MouthCue
                        {
                            Start = startElement.GetDouble(),
                            End = endElement.GetDouble(),
                            Value = valueElement.GetString() ?? "X"
                        });
                    }
                }
            }

            // Post-process the mouth cues for better accuracy
            var processedCues = PostProcessMouthCues(mouthCues);

            // Calculate duration from the last mouth cue's end time
            var duration = processedCues.Count > 0 ? processedCues.Max(c => c.End) : 0.0;

            return Task.FromResult<LipsyncData?>(new LipsyncData
            {
                Metadata = new LipsyncMetadata
                {
                    Duration = Math.Round(duration, 2) // Round to 2 decimal places
                },
                MouthCues = processedCues
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing Rhubarb JSON output");
            return Task.FromResult<LipsyncData?>(null);
        }
    }

    /// <summary>
    /// Post-process mouth cues to improve accuracy and smoothness
    /// </summary>
    private List<MouthCue> PostProcessMouthCues(List<MouthCue> originalCues)
    {
        if (originalCues.Count == 0) return originalCues;

        var processedCues = new List<MouthCue>();
        const double minDuration = 0.05; // Minimum 50ms duration for any cue
        const double maxGap = 0.1; // Maximum 100ms gap between cues

        // Sort cues by start time
        var sortedCues = originalCues.OrderBy(c => c.Start).ToList();

        for (int i = 0; i < sortedCues.Count; i++)
        {
            var currentCue = sortedCues[i];

            // Ensure minimum duration
            if (currentCue.End - currentCue.Start < minDuration)
            {
                currentCue.End = currentCue.Start + minDuration;
            }

            // Fill gaps with neutral mouth shape (X)
            if (i > 0)
            {
                var previousCue = processedCues.Last();
                var gap = currentCue.Start - previousCue.End;

                if (gap > maxGap)
                {
                    // Add a neutral mouth shape to fill the gap
                    processedCues.Add(new MouthCue
                    {
                        Start = previousCue.End,
                        End = currentCue.Start,
                        Value = "X"
                    });
                }
            }

            processedCues.Add(currentCue);
        }

        // Remove very short cues that might cause jitter
        processedCues = processedCues.Where(c => c.End - c.Start >= minDuration).ToList();

        _logger.LogDebug("Post-processed {OriginalCount} cues to {ProcessedCount} cues",
            originalCues.Count, processedCues.Count);

        return processedCues;
    }


}
