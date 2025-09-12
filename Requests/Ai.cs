using System.ComponentModel.DataAnnotations;

namespace cutypai.Requests;

public sealed class ChatRequest
{
    // The user's message to the AI (required)
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Message { get; set; } = string.Empty;
}

public sealed class ChatResponse
{
    public string Message { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool Success { get; set; } = true;
}