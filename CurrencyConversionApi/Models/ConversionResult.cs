namespace CurrencyConversionApi.Models;

/// <summary>
/// Represents the result of a currency conversion operation
/// </summary>
public class ConversionResult
{
    /// <summary>
    /// Original amount to convert
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Source currency code
    /// </summary>
    public required string FromCurrency { get; set; }

    /// <summary>
    /// Target currency code
    /// </summary>
    public required string ToCurrency { get; set; }

    /// <summary>
    /// Converted amount
    /// </summary>
    public decimal ConvertedAmount { get; set; }

    /// <summary>
    /// Exchange rate used for conversion
    /// </summary>
    public decimal ExchangeRate { get; set; }

    /// <summary>
    /// When the conversion was performed
    /// </summary>
    public DateTime ConversionTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the exchange rate was last updated
    /// </summary>
    public DateTime RateLastUpdated { get; set; }

    /// <summary>
    /// Source of the exchange rate
    /// </summary>
    public string RateSource { get; set; } = "Unknown";

    /// <summary>
    /// Request ID for tracing
    /// </summary>
    public string RequestId { get; set; } = Guid.NewGuid().ToString();
}
