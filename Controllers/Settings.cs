using cutypai.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
namespace cutypai.Controllers;

public class SettingsController : Controller
{
    private readonly IUserRepository _users;

    public SettingsController(IUserRepository users)
    {
        _users = users;
    }

    // GET: /settings
    [Route("settings")]
    [Authorize]
    public async Task<IActionResult> Settings(CancellationToken ct)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Auth");
        }

        var user = await _users.GetByIdAsync(userId, ct);
        if (user == null)
        {
            return RedirectToAction("Login", "Auth");
        }

        return View();
    }
}