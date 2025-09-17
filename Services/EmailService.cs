namespace cutypai.Services;

public interface IEmailService
{
    Task<bool> SendPasswordResetEmailAsync(string email, string name, string resetToken, CancellationToken ct = default);
    Task<bool> SendEmailAsync(string to, string subject, string htmlContent, CancellationToken ct = default);
}

public sealed class MailgunEmailService : IEmailService
{
    private readonly ILogger<MailgunEmailService> _logger;
    private readonly string _domain;
    private readonly string _apiKey;
    private readonly string _from;

    public MailgunEmailService(IConfiguration configuration, ILogger<MailgunEmailService> logger)
    {
        _logger = logger;
        _domain = Environment.GetEnvironmentVariable("MAILGUN_DOMAIN") ?? throw new InvalidOperationException("MAILGUN_DOMAIN not configured");
        _apiKey = Environment.GetEnvironmentVariable("MAILGUN_API_KEY") ?? throw new InvalidOperationException("MAILGUN_API_KEY not configured");
        _from = Environment.GetEnvironmentVariable("EMAIL_FROM") ?? throw new InvalidOperationException("EMAIL_FROM not configured");
    }

    public async Task<bool> SendPasswordResetEmailAsync(string email, string name, string resetToken, CancellationToken ct = default)
    {
        var resetUrl = $"https://cutypai.alaminia.com/reset-password?token={resetToken}&email={Uri.EscapeDataString(email)}";

        var htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Password Reset - CutyPAI</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #f8f9fa; padding: 20px; text-align: center; border-radius: 8px 8px 0 0; }}
        .content {{ background-color: #ffffff; padding: 30px; border: 1px solid #dee2e6; }}
        .footer {{ background-color: #f8f9fa; padding: 15px; text-align: center; font-size: 12px; color: #6c757d; border-radius: 0 0 8px 8px; }}
        .btn {{ display: inline-block; padding: 12px 24px; background-color: #007bff; color: white; text-decoration: none; border-radius: 4px; margin: 20px 0; }}
        .btn:hover {{ background-color: #0056b3; }}
        .warning {{ background-color: #fff3cd; border: 1px solid #ffeaa7; padding: 15px; border-radius: 4px; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>CutyPAI - Password Reset</h1>
        </div>
        <div class='content'>
            <h2>Hello {name},</h2>
            <p>We received a request to reset your password for your CutyPAI account. If you made this request, click the button below to reset your password:</p>
            
            <div style='text-align: center;'>
                <a href='{resetUrl}' class='btn'>Reset Your Password</a>
            </div>
            
            <p>Or copy and paste this link into your browser:</p>
            <p style='word-break: break-all; background-color: #f8f9fa; padding: 10px; border-radius: 4px;'>{resetUrl}</p>
            
            <div class='warning'>
                <strong>Important:</strong>
                <ul>
                    <li>This link will expire in 1 hour for security reasons</li>
                    <li>If you didn't request this password reset, please ignore this email</li>
                    <li>Your password won't change until you access the link above and create a new one</li>
                </ul>
            </div>
        </div>
        <div class='footer'>
            <p>This email was sent from CutyPAI. If you have any questions, please contact our support team.</p>
            <p>&copy; 2025 CutyPAI. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

        return await SendEmailAsync(email, "Reset Your CutyPAI Password", htmlContent, ct);
    }

    public async Task<bool> SendEmailAsync(string to, string subject, string htmlContent, CancellationToken ct = default)
    {
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"api:{_apiKey}")));

            var formContent = new List<KeyValuePair<string, string>>
            {
                new("from", _from),
                new("to", to),
                new("subject", subject),
                new("html", htmlContent)
            };

            var content = new FormUrlEncodedContent(formContent);
            var response = await client.PostAsync($"https://api.mailgun.net/v3/{_domain}/messages", content, ct);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Email sent successfully to {Email}", to);
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("Failed to send email to {Email}. Status: {Status}, Response: {Response}",
                    to, response.StatusCode, errorContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email to {Email}", to);
            return false;
        }
    }
}
