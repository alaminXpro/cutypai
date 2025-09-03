using System.Security.Claims;
using cutypai.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace cutypai.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthApiController : ControllerBase
{
    private readonly ITokenService _tokens;
    private readonly IUserRepository _users;

    public AuthApiController(IUserRepository users, ITokenService tokens)
    {
        _users = users;
        _tokens = tokens;
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
        return Ok(token);
    }

    // POST /api/auth/refresh
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshTokenRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var token = await _tokens.RefreshTokenAsync(req.RefreshToken);
        if (token == null) return Unauthorized(new { message = "Invalid or expired refresh token" });

        return Ok(token);
    }

    // POST /api/auth/revoke
    [HttpPost("revoke")]
    [Authorize]
    public async Task<ActionResult> Revoke([FromBody] RefreshTokenRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var success = await _tokens.RevokeTokenAsync(req.RefreshToken);
        if (!success) return BadRequest(new { message = "Failed to revoke token" });

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
            last_login = user.LastLoginUtc
        });
    }
}