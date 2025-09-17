using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using cutypai.Models;
using cutypai.Repositories;
using cutypai.Services;
using System.Security.Claims;

namespace cutypai.Controllers;

[Authorize]
public class SettingsController : Controller
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly IContextBuilderService _contextBuilderService;
    private readonly ISettingsAuditService _auditService;
    private readonly ILogger<SettingsController> _logger;

    public SettingsController(
        ISettingsRepository settingsRepository,
        IContextBuilderService contextBuilderService,
        ISettingsAuditService auditService,
        ILogger<SettingsController> logger)
    {
        _settingsRepository = settingsRepository;
        _contextBuilderService = contextBuilderService;
        _auditService = auditService;
        _logger = logger;
    }

    // GET: Settings
    public async Task<IActionResult> Index()
    {
        try
        {
            var settings = await _settingsRepository.GetActiveSettingsAsync();
            var viewModel = new SettingsViewModel
            {
                SystemPrompt = settings?.SystemPrompt ?? string.Empty,
                Instructions = settings?.Instructions ?? string.Empty,
                UpdatedAt = settings?.UpdatedAt ?? DateTime.UtcNow
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading settings page");
            TempData["ErrorMessage"] = "Failed to load settings. Please try again.";
            return View(new SettingsViewModel());
        }
    }

    // POST: Settings/Update
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(SettingsUpdateRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please check your input and try again.";
                return RedirectToAction(nameof(Index));
            }

            // Get current user info
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                        User.FindFirst("sub")?.Value ??
                        "unknown";
            var userName = User.FindFirst(ClaimTypes.Name)?.Value ??
                          User.Identity?.Name ??
                          "Unknown User";

            // Get current settings for comparison
            var currentSettings = await _settingsRepository.GetActiveSettingsAsync();
            var oldSettings = currentSettings ?? new Settings();

            var newSettings = new Settings
            {
                Id = oldSettings.Id, // Keep the same ID
                SystemPrompt = request.SystemPrompt,
                Instructions = request.Instructions,
                UpdatedAt = DateTime.UtcNow
            };

            var updatedSettings = await _settingsRepository.UpdateSettingsAsync(newSettings);

            if (updatedSettings != null)
            {
                // Log the change for audit
                await _auditService.LogSettingsChangeAsync(
                    oldSettings,
                    newSettings,
                    userId,
                    userName,
                    "updated",
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    Request.Headers.UserAgent.ToString()
                );

                TempData["SuccessMessage"] = "Settings updated successfully!";
                _logger.LogInformation("Settings updated successfully by user {UserId}", userId);
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to update settings. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating settings");
            TempData["ErrorMessage"] = "An error occurred while updating settings. Please try again.";
            return RedirectToAction(nameof(Index));
        }
    }

    // GET: Settings/Preview
    public async Task<IActionResult> Preview(string? userMood = null)
    {
        try
        {
            var settings = await _settingsRepository.GetActiveSettingsAsync();
            if (settings == null)
            {
                return Json(new { error = "No settings found" });
            }

            // Get current user ID
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                        User.FindFirst("sub")?.Value ??
                        "preview-user";

            // Build preview with context
            var previewPrompt = await _contextBuilderService.BuildSystemPromptAsync(settings, userId, userMood);

            return Json(new { preview = previewPrompt });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating preview");
            return Json(new { error = "Failed to generate preview" });
        }
    }

    // GET: Settings/Reset
    public async Task<IActionResult> Reset()
    {
        try
        {
            // Get current user info
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                        User.FindFirst("sub")?.Value ??
                        "unknown";
            var userName = User.FindFirst(ClaimTypes.Name)?.Value ??
                          User.Identity?.Name ??
                          "Unknown User";

            // Get current settings for comparison
            var currentSettings = await _settingsRepository.GetActiveSettingsAsync();
            var oldSettings = currentSettings ?? new Settings();

            var defaultSettings = await _settingsRepository.CreateDefaultSettingsAsync();

            if (defaultSettings != null)
            {
                // Log the reset for audit
                await _auditService.LogSettingsChangeAsync(
                    oldSettings,
                    defaultSettings,
                    userId,
                    userName,
                    "reset",
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    Request.Headers.UserAgent.ToString()
                );

                TempData["SuccessMessage"] = "Settings reset to default values!";
                _logger.LogInformation("Settings reset to default by user {UserId}", userId);
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to reset settings. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting settings");
            TempData["ErrorMessage"] = "Failed to reset settings. Please try again.";
            return RedirectToAction(nameof(Index));
        }
    }

    // GET: Settings/History
    public async Task<IActionResult> History()
    {
        try
        {
            var settings = await _settingsRepository.GetActiveSettingsAsync();
            if (settings?.Id == null)
            {
                TempData["ErrorMessage"] = "No settings found to show history for.";
                return RedirectToAction(nameof(Index));
            }

            var auditHistory = await _auditService.GetSettingsHistoryAsync(settings.Id, 50);
            return View(auditHistory);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading settings history");
            TempData["ErrorMessage"] = "Failed to load settings history. Please try again.";
            return RedirectToAction(nameof(Index));
        }
    }
}

public class SettingsViewModel
{
    public string SystemPrompt { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
}
