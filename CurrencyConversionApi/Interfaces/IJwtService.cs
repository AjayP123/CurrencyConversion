using CurrencyConversionApi.Models;

namespace CurrencyConversionApi.Interfaces;

/// <summary>
/// Interface for JWT token operations
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// Generate JWT access token for user
    /// </summary>
    string GenerateAccessToken(User user);
    
    /// <summary>
    /// Generate refresh token
    /// </summary>
    string GenerateRefreshToken();
    
    /// <summary>
    /// Validate and parse JWT token
    /// </summary>
    bool ValidateToken(string token, out string? userId, out List<string> roles);
    
    /// <summary>
    /// Get token expiration time
    /// </summary>
    DateTime GetTokenExpiration();
}
