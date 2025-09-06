using Google.Apis.Auth;
using Microsoft.Extensions.Options;
using cutypai.Models;

namespace cutypai.Services;

public interface IGoogleTokenVerificationService
{
    Task<GoogleUserInfo?> VerifyTokenAsync(string token);
}

public class GoogleTokenVerificationService : IGoogleTokenVerificationService
{
    private readonly GoogleOAuthSettings _settings;

    public GoogleTokenVerificationService(IOptions<GoogleOAuthSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task<GoogleUserInfo?> VerifyTokenAsync(string token)
    {
        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(token, new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { _settings.ClientId }
            });
            return new GoogleUserInfo
            {
                Id = payload.Subject,
                Email = payload.Email,
                Name = payload.Name,
                Picture = payload.Picture
            };
        }
        catch
        {
            return null;
        }
    }
}

public class GoogleUserInfo
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Picture { get; set; } = string.Empty;
}
