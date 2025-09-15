using System.ComponentModel.DataAnnotations;

namespace cutypai.Requests;

public sealed class ChatRequest
{
    // The user's message to the AI (required)
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Message { get; set; } = string.Empty;
}
