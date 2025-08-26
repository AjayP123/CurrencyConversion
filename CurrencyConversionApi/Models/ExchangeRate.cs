namespace CurrencyConversionApi.Models;

/// <summary>
/// Represents an exchange rate between two currencies
/// </summary>
public class ExchangeRate
{
    /// <summary>
    /// Source currency code
    /// </summary>
    public required string FromCurrency { get; set; }

    /// <summary>
    /// Target currency code
    /// </summary>
    public required string ToCurrency { get; set; }

    /// <summary>
    /// Exchange rate value
    /// </summary>
    public decimal Rate { get; set; }

    /// <summary>
    /// When this rate was last updated
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Source of this exchange rate (e.g., "ExchangeRateAPI", "CentralBank")
    /// </summary>
    public string Source { get; set; } = "Unknown";

    /// <summary>
    /// Composite key for indexing
    /// </summary>
    public string CurrencyPair => $"{FromCurrency}-{ToCurrency}";
}
