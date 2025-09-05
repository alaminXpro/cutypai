using System.ComponentModel.DataAnnotations;

namespace cutypai.Models;

public sealed class RegisterRequest
{
    [Required] [StringLength(100)] public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(320)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(200, MinimumLength = 8)] // Increased minimum length
    public string Password { get; set; } = string.Empty;

    public string? AvatarUrl { get; set; }
}

public sealed class LoginRequest
{
    [Required]
    [EmailAddress]
    [StringLength(320)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(200, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;
}

public sealed class RefreshTokenRequest
{
    public string? RefreshToken { get; set; } = string.Empty;
}

public sealed class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty; // Added refresh token
    public DateTime ExpiresAtUtc { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public sealed class PasswordValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
}