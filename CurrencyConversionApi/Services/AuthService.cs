using CurrencyConversionApi.Interfaces;
using CurrencyConversionApi.Models;
using System.Security.Cryptography;
using System.Text;

namespace CurrencyConversionApi.Services;

/// <summary>
/// Authentication service with in-memory user store (demo purposes)
/// In production, this would connect to a database
/// </summary>
public class AuthService : IAuthService
{
    private readonly IJwtService _jwtService;
    private readonly ILogger<AuthService> _logger;
    
    // Demo users - In production, this would be a database
    private static readonly List<User> _users = new()
    {
        new User
        {
            Id = "1",
            Username = "admin",
            Email = "admin@currencyapi.com",
            PasswordHash = HashPassword("admin123"),
            Roles = new List<string> { UserRoles.Admin }
        },
        new User
        {
            Id = "2", 
            Username = "premium",
            Email = "premium@example.com",
            PasswordHash = HashPassword("premium123"),
            Roles = new List<string> { UserRoles.Premium }
        },
        new User
        {
            Id = "3",
            Username = "basic",
            Email = "basic@example.com", 
            PasswordHash = HashPassword("basic123"),
            Roles = new List<string> { UserRoles.Basic }
        }
    };

    public AuthService(IJwtService jwtService, ILogger<AuthService> logger)
    {
        _jwtService = jwtService;
        _logger = logger;
    }

    public async Task<User?> AuthenticateAsync(string username, string password)
    {
        _logger.LogDebug("Attempting authentication for user: {Username}", username);
        
        var user = await GetUserByUsernameAsync(username);
        if (user == null)
        {
            _logger.LogWarning("Authentication failed: User not found - {Username}", username);
            return null;
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Authentication failed: User inactive - {Username}", username);
            return null;
        }

        if (!VerifyPassword(password, user.PasswordHash))
        {
            _logger.LogWarning("Authentication failed: Invalid password - {Username}", username);
            return null;
        }

        // Update last login
        user.LastLoginAt = DateTime.UtcNow;
        
        _logger.LogInformation("User authenticated successfully: {Username} with roles [{Roles}]", 
            username, string.Join(", ", user.Roles));
        
        return user;
    }

    public Task<User?> GetUserByUsernameAsync(string username)
    {
        var user = _users.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(user);
    }

    public async Task<TokenResponse> GenerateTokenAsync(User user)
    {
        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();
        var expiresAt = _jwtService.GetTokenExpiration();

        _logger.LogDebug("Generated tokens for user: {Username}", user.Username);

        return await Task.FromResult(new TokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt,
            TokenType = "Bearer",
            Roles = user.Roles
        });
    }

    public List<object> GetDemoUsers()
    {
        return _users.Select(u => (object)new 
        {
            username = u.Username,
            email = u.Email,
            roles = u.Roles
        }).ToList();
    }

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "CurrencyAPISalt"));
        return Convert.ToBase64String(hashedBytes);
    }

    private static bool VerifyPassword(string password, string hashedPassword)
    {
        var hashToVerify = HashPassword(password);
        return hashToVerify == hashedPassword;
    }
}
