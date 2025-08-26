using CurrencyConversionApi.Models;

namespace CurrencyConversionApi.Interfaces;

/// <summary>
/// Interface for user authentication operations
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Authenticate user with username and password
    /// </summary>
    Task<User?> AuthenticateAsync(string username, string password);
    
    /// <summary>
    /// Get user by username
    /// </summary>
    Task<User?> GetUserByUsernameAsync(string username);
    
    /// <summary>
    /// Generate token response for authenticated user
    /// </summary>
    Task<TokenResponse> GenerateTokenAsync(User user);
}
