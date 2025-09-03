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

        var existing = await _users.GetByEmailAsync(req.Email, ct);
        if (existing is not null)
            return Conflict(new { message = "Email already registered." });

        var user = new User
        {
            Name = req.Name,
            Email = req.Email,
            Password = req.Password, // will be hashed by repository RegisterAsync
            AvatarUrl = req.AvatarUrl,
            Role = UserRole.User,
            Status = UserStatus.Active,
            CreatedAtUtc = DateTime.UtcNow
        };

        user = await _users.RegisterAsync(user, ct); // hashes password

        var token = _tokens.CreateAccessToken(user);
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

        await _users.SetLastLoginAsync(user.Id!, DateTime.UtcNow, ct);
        var token = _tokens.CreateAccessToken(user);
        return Ok(token);
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