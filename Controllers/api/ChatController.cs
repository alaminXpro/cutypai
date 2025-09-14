using System.Security.Claims;
using cutypai.Models;
using cutypai.Requests;
using cutypai.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace cutypai.Controllers.api;

[ApiController]
[Route("api/ai")]
public sealed class ChatApiController : ControllerBase
{
    private readonly IAiService _aiService;
    private readonly ILogger<ChatApiController> _logger;
    private readonly IUserRepository _users;

    public ChatApiController(IUserRepository users, IAiService aiService, ILogger<ChatApiController> logger)
    {
        _users = users;
        _aiService = aiService;
        _logger = logger;
    }

    // POST /api/ai/chat  (protected)
    [HttpPost("chat")]
    [Authorize]
    public async Task<ActionResult<ChatResponse>> Chat([FromBody] ChatRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        try
        {
            // Extract user ID from JWT token claims
            var sub = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(sub))
            {
                _logger.LogWarning("Unauthorized chat request - no user ID in token");
                return Unauthorized("Invalid token");
            }

            // Get user information from database
            var user = await _users.GetByIdAsync(sub, ct);
            if (user is null)
            {
                _logger.LogWarning("Chat request for non-existent user {UserId}", sub);
                return NotFound("User not found");
            }

            _logger.LogInformation("Processing chat request from user {UserId} ({UserName})", user.Id, user.Name);

            // Process the chat message through AI service with TTS
            var response = await _aiService.ProcessChatMessageWithIndividualAudioAsync(
                request.Message, user.Id!, request.IncludeAudio, ct);


            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat request");
            return StatusCode(500, new ChatResponse
            {
                Message = "An error occurred while processing your request. Please try again.",
                Success = false,
                HasAudio = false
            });
        }
    }

    // POST /api/ai/chat-individual-audio  (protected)
    [HttpPost("chat-individual-audio")]
    [Authorize]
    public async Task<ActionResult<string>> ChatWithIndividualAudio([FromBody] ChatRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        try
        {
            // Extract user ID from JWT token claims
            var sub = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(sub))
            {
                _logger.LogWarning("Unauthorized chat request - no user ID in token");
                return Unauthorized("Invalid token");
            }

            // Get user information from database
            var user = await _users.GetByIdAsync(sub, ct);
            if (user is null)
            {
                _logger.LogWarning("Chat request for non-existent user {UserId}", sub);
                return NotFound("User not found");
            }

            _logger.LogInformation("Processing chat request with individual audio from user {UserId} ({UserName})", user.Id, user.Name);

            // Process the chat message through AI service with individual message audio
            var aiResponse = await _aiService.ProcessChatMessageWithIndividualAudioAsync(
                request.Message, user.Id!, request.IncludeAudio, ct);

            _logger.LogInformation("Successfully processed chat request with individual audio for user {UserId}",
                user.Id);
            return Ok(aiResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat request with individual audio");
            return StatusCode(500, "An error occurred while processing your request. Please try again.");
        }
    }

}