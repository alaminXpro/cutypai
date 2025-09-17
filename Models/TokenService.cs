using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace cutypai.Models;

public interface ITokenService
{
    Task<AuthResponse> CreateTokensAsync(User user);
    Task<AuthResponse?> RefreshTokenAsync(string refreshToken);
    Task<bool> RevokeTokenAsync(string refreshToken);
    Task<bool> RevokeAllUserTokensAsync(string userId);
}

public sealed class TokenService : ITokenService
{
    private readonly JwtSettings _jwt;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserRepository _userRepository;

    public TokenService(IOptions<JwtSettings> jwtOptions, IRefreshTokenRepository refreshTokenRepository,
        IUserRepository userRepository)
    {
        _jwt = jwtOptions.Value;
        _refreshTokenRepository = refreshTokenRepository;
        _userRepository = userRepository;
    }

    public async Task<AuthResponse> CreateTokensAsync(User user)
    {
        var accessToken = CreateAccessToken(user);
        var refreshTokenExpiry = DateTime.UtcNow.AddDays(_jwt.RefreshTokenDays);
        var refreshToken = await _refreshTokenRepository.CreateAsync(user.Id!, refreshTokenExpiry);

        return new AuthResponse
        {
            AccessToken = accessToken.Token,
            RefreshToken = refreshToken?.Token ?? string.Empty,
            ExpiresAtUtc = accessToken.ExpiresAt,
            UserId = user.Id ?? string.Empty,
            Email = user.Email,
            Name = user.Name,
            Role = user.Role.ToString()
        };
    }

    public async Task<AuthResponse?> RefreshTokenAsync(string refreshToken)
    {
        var storedToken = await _refreshTokenRepository.GetByTokenAsync(refreshToken);

        if (storedToken == null || !storedToken.IsActive) return null;

        // Get the user associated with this refresh token
        var user = await _userRepository.GetByIdAsync(storedToken.UserId);
        if (user == null || user.Status != UserStatus.Active)
        {
            await _refreshTokenRepository.RevokeTokenAsync(refreshToken, "User not found or inactive");
            return null;
        }

        // Revoke the old refresh token
        await _refreshTokenRepository.RevokeTokenAsync(refreshToken, "Replaced by new token");

        // Create new tokens
        return await CreateTokensAsync(user);
    }

    public Task<bool> RevokeTokenAsync(string refreshToken)
    {
        return _refreshTokenRepository.RevokeTokenAsync(refreshToken, "Manually revoked");
    }

    public Task<bool> RevokeAllUserTokensAsync(string userId)
    {
        return _refreshTokenRepository.RevokeAllUserTokensAsync(userId);
    }

    private (string Token, DateTime ExpiresAt) CreateAccessToken(User user)
    {
        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(_jwt.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id ?? string.Empty),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Role, user.Role.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now).ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            _jwt.Issuer,
            _jwt.Audience,
            claims,
            now,
            expires,
            creds
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}