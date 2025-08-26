namespace CurrencyConversionApi.Configuration;

/// <summary>
/// Configuration for exchange rate providers
/// </summary>
public class ExchangeRateConfig
{
    public const string SectionName = "ExchangeRates";

    /// <summary>
    /// Primary exchange rate provider
    /// </summary>
    public string PrimaryProvider { get; set; } = "ExchangeRateAPI";

    /// <summary>
    /// Fallback providers in order of preference
    /// </summary>
    public List<string> FallbackProviders { get; set; } = new();

    /// <summary>
    /// Cache duration for exchange rates
    /// </summary>
    public TimeSpan CacheDuration { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Timeout for external API calls
    /// </summary>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Maximum retry attempts for failed requests
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Provider-specific configurations
    /// </summary>
    public Dictionary<string, ProviderConfig> Providers { get; set; } = new();
}

/// <summary>
/// Configuration for individual provider
/// </summary>
public class ProviderConfig
{
    /// <summary>
    /// API base URL
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// API key (should be in user secrets or environment variables)
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Rate limit per minute
    /// </summary>
    public int RateLimitPerMinute { get; set; } = 60;

    /// <summary>
    /// Whether this provider is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Provider priority (lower is higher priority)
    /// </summary>
    public int Priority { get; set; } = 1;
}
