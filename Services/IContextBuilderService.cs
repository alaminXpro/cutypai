using cutypai.Models;

namespace cutypai.Services;

public interface IContextBuilderService
{
    Task<string> BuildSystemPromptAsync(Settings settings, string userId, string? userMood = null);
    Task<UserContext> GetUserContextAsync(string userId);
    string InjectContextVariables(string template, string userName, string currentTime, string? userMood = null);
    string BuildConversationContext(string message, UserContext userContext, int maxHistoryLength = 5);
}

public class UserContext
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string CurrentTime { get; set; } = string.Empty;
    public string? UserMood { get; set; }
}
