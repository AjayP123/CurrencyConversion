namespace CurrencyConversionApi.Models;

/// <summary>
/// Represents a currency with its metadata
/// </summary>
public class Currency
{
    /// <summary>
    /// Three-letter currency code (ISO 4217)
    /// </summary>
    public required string Code { get; set; }

    /// <summary>
    /// Full name of the currency
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Currency symbol
    /// </summary>
    public required string Symbol { get; set; }

    /// <summary>
    /// Number of decimal places typically used for this currency
    /// </summary>
    public int DecimalPlaces { get; set; } = 2;

    /// <summary>
    /// Whether this currency is actively supported
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Last updated timestamp
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
