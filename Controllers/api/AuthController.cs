using System.Security.Claims;
using cutypai.Models;
using cutypai.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;

namespace cutypai.Controllers.api;

[ApiController]
[Route("api/auth")]
public sealed class AuthApiController : ControllerBase
{
    private readonly ITokenService _tokens;
    private readonly IUserRepository _users;
    private readonly IGoogleTokenVerificationService _googleTokenService;

    public AuthApiController(IUserRepository users, ITokenService tokens, IGoogleTokenVerificationService googleTokenService)
    {
        _users = users;
        _tokens = tokens;
        _googleTokenService = googleTokenService;
    }

    // POST /api/auth/register
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var user = new User
        {
            Name = req.Name,
            Email = req.Email,
            Password = req.Password, // will be hashed by repository RegisterAsync with validation
            AvatarUrl = req.AvatarUrl,
            Role = UserRole.User,
            Status = UserStatus.Active,
            CreatedAtUtc = DateTime.UtcNow
        };

        var (registeredUser, validationResult) = await _users.RegisterAsync(user, ct);

        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors) ModelState.AddModelError("Password", error);
            return ValidationProblem(ModelState);
        }

        if (registeredUser == null) return BadRequest(new { message = "Registration failed" });

        var token = await _tokens.CreateTokensAsync(registeredUser);

        // Set refresh token as HTTP-only cookie
        SetRefreshTokenCookie(token.RefreshToken);

        return Ok(token);
    }

    // POST /api/auth/login
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var user = await _users.AuthenticateAsync(req.Email, req.Password, ct);
        if (user is null) return Unauthorized(new { message = "Invalid credentials." });

        var token = await _tokens.CreateTokensAsync(user);

        // Set refresh token as HTTP-only cookie
        SetRefreshTokenCookie(token.RefreshToken);

        return Ok(token);
    }

    // POST /api/auth/refresh
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshTokenRequest? req, CancellationToken ct)
    {
        // Try to get refresh token from cookies first, then from request body
        var refreshToken = Request.Cookies["refreshToken"] ?? req?.RefreshToken;

        if (string.IsNullOrWhiteSpace(refreshToken))
            return BadRequest(new { message = "Refresh token is required either in cookies or request body" });

        var token = await _tokens.RefreshTokenAsync(refreshToken);
        if (token == null)
        {
            // Clear the cookie if token is invalid
            ClearRefreshTokenCookie();
            return Unauthorized(new { message = "Invalid or expired refresh token" });
        }

        // Set new refresh token as HTTP-only cookie
        SetRefreshTokenCookie(token.RefreshToken);

        return Ok(token);
    }

    // POST /api/auth/revoke
    [HttpPost("revoke")]
    [Authorize]
    public async Task<ActionResult> Revoke([FromBody] RefreshTokenRequest? req, CancellationToken ct)
    {
        // Try to get refresh token from cookies first, then from request body
        var refreshToken = Request.Cookies["refreshToken"] ?? req?.RefreshToken;

        if (string.IsNullOrWhiteSpace(refreshToken))
            return BadRequest(new { message = "Refresh token is required either in cookies or request body" });

        var success = await _tokens.RevokeTokenAsync(refreshToken);
        if (!success) return BadRequest(new { message = "Failed to revoke token" });

        // Clear the cookie after revoking
        ClearRefreshTokenCookie();

        return Ok(new { message = "Token revoked successfully" });
    }

    // POST /api/auth/revoke-all
    [HttpPost("revoke-all")]
    [Authorize]
    public async Task<ActionResult> RevokeAll(CancellationToken ct)
    {
        var sub = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(sub)) return Unauthorized();

        var success = await _tokens.RevokeAllUserTokensAsync(sub);
        if (!success) return BadRequest(new { message = "Failed to revoke tokens" });

        // Clear the cookie after revoking all tokens
        ClearRefreshTokenCookie();

        return Ok(new { message = "All tokens revoked successfully" });
    }

    // GET /api/auth/me  (protected example)
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<object>> Me(CancellationToken ct)
    {
        // read claims from token
        var sub = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(sub)) return Unauthorized();

        var user = await _users.GetByIdAsync(sub!, ct);
        if (user is null) return NotFound();

        return Ok(new
        {
            id = user.Id,
            name = user.Name,
            email = user.Email,
            role = user.Role.ToString(),
            status = user.Status.ToString(),
            avatar_url = user.AvatarUrl,
            created_at = user.CreatedAtUtc,
            last_login = user.LastLoginUtc,
            preferences = user.Preferences?.ToJson() ?? "{}"
        });
    }

    private void SetRefreshTokenCookie(string refreshToken)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps ||
                     !HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment(),
            SameSite = SameSiteMode.Lax, // Better compatibility than Strict
            Expires = DateTime.UtcNow.AddDays(7), // Match your refresh token expiry
            Path = "/",
            Domain = null // Let browser determine domain automatically
        };

        Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
    }

    private void ClearRefreshTokenCookie()
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps ||
                     !HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment(),
            SameSite = SameSiteMode.Lax,
            Expires = DateTime.UtcNow.AddDays(-1), // Expire the cookie
            Path = "/",
            Domain = null
        };

        Response.Cookies.Append("refreshToken", "", cookieOptions);
    }

    // POST /api/auth/sso/google
    [HttpPost("sso/google")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> GoogleSso([FromBody] GoogleSsoRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Token))
            return BadRequest(new { message = "Google token is required" });

        var googleUser = await _googleTokenService.VerifyTokenAsync(req.Token);
        if (googleUser == null)
            return Unauthorized(new { message = "Invalid Google token" });

        // Find existing user by email or external ID
        var user = await _users.GetByEmailAsync(googleUser.Email) ?? 
                   await _users.FindByExternalIdAsync("google", googleUser.Id);

        if (user == null)
        {
            // Create new user from Google data
            user = await _users.CreateFromSsoAsync(
                googleUser.Email, 
                googleUser.Name, 
                "google", 
                googleUser.Id, 
                googleUser.Picture, 
                ct
            );
        }
        else
        {
            // Update existing user with Google info if needed
            if (string.IsNullOrEmpty(user.AvatarUrl))
                user.AvatarUrl = googleUser.Picture;
            
            user.LastLoginUtc = DateTime.UtcNow;
            await _users.UpdateAsync(user, ct);
        }

        var token = await _tokens.CreateTokensAsync(user);
        SetRefreshTokenCookie(token.RefreshToken);

        return Ok(token);
    }

    public class GoogleSsoRequest
    {
        public string Token { get; set; } = string.Empty;
    }
}