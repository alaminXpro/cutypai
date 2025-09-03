using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using cutypai.Models;

namespace cutypai.Controllers;

public class AuthController : Controller
{
    private readonly IUserRepository _users;
    private readonly ITokenService _tokens;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IUserRepository users, ITokenService tokens, ILogger<AuthController> logger)
    {
        _users = users;
        _tokens = tokens;
        _logger = logger;
    }

    // GET: /login
    [Route("login")]
    [AllowAnonymous]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }
        return View();
    }

    // POST: /login
    [HttpPost]
    [Route("login")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginRequest model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var user = await _users.AuthenticateAsync(model.Email, model.Password, ct);
            if (user == null)
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return View(model);
            }

            // Create claims for cookie authentication
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id!),
                new(ClaimTypes.Name, user.Name),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, user.Role.ToString())
            };

            var claimsIdentity = new ClaimsIdentity(claims, "Cookies");
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            };

            await HttpContext.SignInAsync("Cookies", new ClaimsPrincipal(claimsIdentity), authProperties);

            _logger.LogInformation("User {Email} logged in successfully", user.Email);
            
            TempData["SuccessMessage"] = "Welcome back! You have successfully logged in.";
            return RedirectToAction("Index", "Home");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for user {Email}", model.Email);
            ModelState.AddModelError("", "An error occurred during login. Please try again.");
            return View(model);
        }
    }

    // GET: /signup
    [Route("signup")]
    [AllowAnonymous]
    public IActionResult Signup()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }
        return View();
    }

    // POST: /signup
    [HttpPost]
    [Route("signup")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Signup(RegisterRequest model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var user = new User
            {
                Name = model.Name,
                Email = model.Email,
                Password = model.Password,
                AvatarUrl = model.AvatarUrl,
                Role = UserRole.User,
                Status = UserStatus.Active,
                CreatedAtUtc = DateTime.UtcNow
            };

            var (registeredUser, validationResult) = await _users.RegisterAsync(user, ct);

            if (!validationResult.IsValid)
            {
                foreach (var error in validationResult.Errors)
                {
                    ModelState.AddModelError("Password", error);
                }
                return View(model);
            }

            if (registeredUser == null)
            {
                ModelState.AddModelError("", "Registration failed. Please try again.");
                return View(model);
            }

            // Auto-login after successful registration
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, registeredUser.Id!),
                new(ClaimTypes.Name, registeredUser.Name),
                new(ClaimTypes.Email, registeredUser.Email),
                new(ClaimTypes.Role, registeredUser.Role.ToString())
            };

            var claimsIdentity = new ClaimsIdentity(claims, "Cookies");
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            };

            await HttpContext.SignInAsync("Cookies", new ClaimsPrincipal(claimsIdentity), authProperties);

            _logger.LogInformation("User {Email} registered and logged in successfully", registeredUser.Email);
            
            TempData["SuccessMessage"] = "Welcome! Your account has been created successfully.";
            return RedirectToAction("Index", "Home");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for user {Email}", model.Email);
            ModelState.AddModelError("", "An error occurred during registration. Please try again.");
            return View(model);
        }
    }

    // POST: /logout
    [HttpPost]
    [Route("logout")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
        
        await HttpContext.SignOutAsync("Cookies");
        
        _logger.LogInformation("User {Email} logged out", userEmail);
        
        TempData["InfoMessage"] = "You have been logged out successfully.";
        return RedirectToAction("Index", "Home");
    }

    // GET: /profile
    [Route("profile")]
    [Authorize]
    public async Task<IActionResult> Profile(CancellationToken ct)
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

        return View(user);
    }
}