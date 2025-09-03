namespace cutypai.Models;

public sealed class JwtSettings
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty; // HMAC secret
    public int AccessTokenMinutes { get; set; } = 15; // Reduced to 15 minutes for better security
    public int RefreshTokenDays { get; set; } = 7; // Refresh token valid for 7 days
    public int ClockSkewSeconds { get; set; } = 30; // Clock skew tolerance

    /// <summary>
    ///     Validates that the JWT key meets security requirements
    /// </summary>
    public bool IsKeySecure()
    {
        return !string.IsNullOrWhiteSpace(Key) && Key.Length >= 32;
    }
}