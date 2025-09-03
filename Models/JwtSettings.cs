namespace cutypai.Models;

public sealed class JwtSettings
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty; // HMAC secret
    public int AccessTokenMinutes { get; set; } = 60; // default 60m
}