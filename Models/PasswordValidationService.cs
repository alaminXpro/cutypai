using System.Text.RegularExpressions;

namespace cutypai.Models;

public interface IPasswordValidationService
{
    PasswordValidationResult ValidatePassword(string password);
}

public sealed class PasswordValidationService : IPasswordValidationService
{
    public PasswordValidationResult ValidatePassword(string password)
    {
        var result = new PasswordValidationResult { IsValid = true };

        if (string.IsNullOrWhiteSpace(password))
        {
            result.Errors.Add("Password is required");
            result.IsValid = false;
            return result;
        }

        // Minimum length check
        if (password.Length < 8)
        {
            result.Errors.Add("Password must be at least 8 characters long");
            result.IsValid = false;
        }

        // Maximum length check
        if (password.Length > 200)
        {
            result.Errors.Add("Password must not exceed 200 characters");
            result.IsValid = false;
        }

        // Must contain at least one uppercase letter
        if (!Regex.IsMatch(password, @"[A-Z]"))
        {
            result.Errors.Add("Password must contain at least one uppercase letter");
            result.IsValid = false;
        }

        // Must contain at least one lowercase letter
        if (!Regex.IsMatch(password, @"[a-z]"))
        {
            result.Errors.Add("Password must contain at least one lowercase letter");
            result.IsValid = false;
        }

        // Must contain at least one digit
        if (!Regex.IsMatch(password, @"\d"))
        {
            result.Errors.Add("Password must contain at least one number");
            result.IsValid = false;
        }

        // Must contain at least one special character
        if (!Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?~`]"))
        {
            result.Errors.Add("Password must contain at least one special character");
            result.IsValid = false;
        }

        // Check for common weak passwords
        var commonPasswords = new[]
        {
            "password", "123456", "12345678", "qwerty", "abc123",
            "password123", "admin", "letmein", "welcome", "monkey"
        };

        if (commonPasswords.Any(cp => string.Equals(cp, password, StringComparison.OrdinalIgnoreCase)))
        {
            result.Errors.Add("Password is too common. Please choose a more secure password");
            result.IsValid = false;
        }

        return result;
    }
}