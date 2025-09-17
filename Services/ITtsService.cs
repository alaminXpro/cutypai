namespace cutypai.Services;

public interface ITtsService
{
    Task<byte[]> ConvertTextToSpeechAsync(string text, CancellationToken ct = default);
    Task<string> ConvertTextToSpeechBase64Async(string text, CancellationToken ct = default);
}