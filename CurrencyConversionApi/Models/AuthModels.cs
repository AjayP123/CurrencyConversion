namespace CurrencyConversionApi.Models;

/// <summary>
/// User model for authentication
/// </summary>
public class User
{
    public required string Id { get; set; }
    public required string Username { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public required List<string> Roles { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
}

/// <summary>
/// User roles enum
/// </summary>
public static class UserRoles
{
    public const string Admin = "Admin";
    public const string Premium = "Premium";
    public const string Basic = "Basic";
    
    public static readonly List<string> AllRoles = new() { Admin, Premium, Basic };
}

/// <summary>
/// Login request model
/// </summary>
public class LoginRequest
{
    public required string Username { get; set; }
    public required string Password { get; set; }
}

/// <summary>
/// JWT token response
/// </summary>
public class TokenResponse
{
    public required string AccessToken { get; set; }
    public required string RefreshToken { get; set; }
    public DateTime ExpiresAt { get; set; }
    public required string TokenType { get; set; } = "Bearer";
    public required List<string> Roles { get; set; }
}
