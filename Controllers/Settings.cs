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
        [Route("articles")]
        [Route("billing")]
        [Route("email")]
        [Route("integrations")]
        [Route("plan")]
        [Authorize]
        public async Task<IActionResult> Settings(CancellationToken ct)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login");
            }

            var user = await _users.GetByIdAsync(userId, ct);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            return View(Settings);
        }
    public IActionResult Articles()
    {
        return View();
    }
    public IActionResult Billing()
    {
        return View();
    }
    public IActionResult Email()
    {
        return View();
    }
    public IActionResult Integrations()
    {
        return View();
    }
    public IActionResult Plan()
    {
        return View();
    }

}
