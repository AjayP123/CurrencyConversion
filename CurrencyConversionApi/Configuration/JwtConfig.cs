namespace CurrencyConversionApi.Configuration;

/// <summary>
/// JWT authentication configuration
/// </summary>
public class JwtConfig
{
    public const string SectionName = "Jwt";
    
    /// <summary>
    /// JWT secret key for signing tokens
    /// </summary>
    public required string SecretKey { get; set; }
    
    /// <summary>
    /// JWT issuer
    /// </summary>
    public required string Issuer { get; set; }
    
    /// <summary>
    /// JWT audience
    /// </summary>
    public required string Audience { get; set; }
    
    /// <summary>
    /// Token expiration time in minutes
    /// </summary>
    public int ExpirationMinutes { get; set; } = 60;
    
    /// <summary>
    /// Refresh token expiration time in days
    /// </summary>
    public int RefreshTokenExpirationDays { get; set; } = 7;
}
