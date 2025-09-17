using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using cutypai.Models;
using cutypai.Services;

namespace cutypai.Controllers;

public class AuthController : Controller
{
    private readonly IUserRepository _users;
    private readonly ITokenService _tokens;
    private readonly ILogger<AuthController> _logger;
    private readonly IEmailService _emailService;
    private readonly IPasswordResetTokenRepository _passwordResetTokens;

    public AuthController(IUserRepository users, ITokenService tokens, ILogger<AuthController> logger, 
        IEmailService emailService, IPasswordResetTokenRepository passwordResetTokens)
    {
        _users = users;
        _tokens = tokens;
        _logger = logger;
        _emailService = emailService;
        _passwordResetTokens = passwordResetTokens;
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
                foreach (var error in validationResult.PasswordErrors)
                {
                    ModelState.AddModelError("Password", error);
                }
                foreach (var error in validationResult.EmailErrors)
                {
                    ModelState.AddModelError("Email", error);
                }
                foreach (var error in validationResult.GeneralErrors)
                {
                    ModelState.AddModelError("", error);
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

    // GET: /forget-password
    [Route("forget-password")]
    [AllowAnonymous]
    public IActionResult ForgetPassword()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }
        return View();
    }

    // POST: /forget-password
    [HttpPost]
    [Route("forget-password")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgetPassword(ForgetPasswordRequest model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var user = await _users.GetByEmailAsync(model.Email, ct);
            
            // Always show success message to prevent email enumeration
            TempData["SuccessMessage"] = "If an account with that email exists, we've sent you a password reset link.";
            
            if (user != null)
            {
                // Clean up any existing tokens for this user
                await _passwordResetTokens.DeleteUserTokensAsync(user.Id!, ct);
                
                // Create new reset token
                var resetToken = await _passwordResetTokens.CreateTokenAsync(user.Id!, user.Email, ct);
                
                // Send reset email
                var emailSent = await _emailService.SendPasswordResetEmailAsync(user.Email, user.Name, resetToken.Token, ct);
                
                if (emailSent)
                {
                    _logger.LogInformation("Password reset email sent to {Email}", user.Email);
                }
                else
                {
                    _logger.LogError("Failed to send password reset email to {Email}", user.Email);
                }
            }
            else
            {
                _logger.LogInformation("Password reset requested for non-existent email: {Email}", model.Email);
            }

            return RedirectToAction("ForgetPasswordConfirmation");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing forget password request for {Email}", model.Email);
            ModelState.AddModelError("", "An error occurred while processing your request. Please try again.");
            return View(model);
        }
    }

    // GET: /forget-password-confirmation
    [Route("forget-password-confirmation")]
    [AllowAnonymous]
    public IActionResult ForgetPasswordConfirmation()
    {
        return View();
    }

    // GET: /reset-password
    [Route("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(string token, string email, CancellationToken ct)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
        {
            TempData["ErrorMessage"] = "Invalid reset link. Please request a new password reset.";
            return RedirectToAction("ForgetPassword");
        }

        // Validate the token
        var resetToken = await _passwordResetTokens.GetValidTokenAsync(token, email, ct);
        if (resetToken == null)
        {
            TempData["ErrorMessage"] = "This reset link has expired or is invalid. Please request a new password reset.";
            return RedirectToAction("ForgetPassword");
        }

        var model = new ResetPasswordRequest
        {
            Token = token,
            Email = email
        };

        return View(model);
    }

    // POST: /reset-password
    [HttpPost]
    [Route("reset-password")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            // Validate the token
            var resetToken = await _passwordResetTokens.GetValidTokenAsync(model.Token, model.Email, ct);
            if (resetToken == null)
            {
                ModelState.AddModelError("", "This reset link has expired or is invalid. Please request a new password reset.");
                return View(model);
            }

            // Get the user
            var user = await _users.GetByEmailAsync(model.Email, ct);
            if (user == null)
            {
                ModelState.AddModelError("", "User not found.");
                return View(model);
            }

            // Reset the password
            var passwordResetSuccess = await _users.ResetPasswordAsync(user.Id!, model.Password, ct);
            if (!passwordResetSuccess)
            {
                ModelState.AddModelError("Password", "Password does not meet security requirements.");
                return View(model);
            }

            // Mark token as used
            await _passwordResetTokens.MarkTokenAsUsedAsync(resetToken.Id!, ct);

            // Delete all other tokens for this user
            await _passwordResetTokens.DeleteUserTokensAsync(user.Id!, ct);

            _logger.LogInformation("Password reset successfully for user {Email}", user.Email);
            
            TempData["SuccessMessage"] = "Your password has been reset successfully. You can now log in with your new password.";
            return RedirectToAction("Login");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password for {Email}", model.Email);
            ModelState.AddModelError("", "An error occurred while resetting your password. Please try again.");
            return View(model);
        }
    }
}