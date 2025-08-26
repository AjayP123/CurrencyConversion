namespace CurrencyConversionApi.DTOs;

/// <summary>
/// Response DTO for currency conversion
/// </summary>
public class ConversionResponseDto
{
    /// <summary>
    /// Original amount
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
    /// Exchange rate used
    /// </summary>
    public decimal ExchangeRate { get; set; }

    /// <summary>
    /// Timestamp of conversion
    /// </summary>
    public DateTime ConversionTime { get; set; }

    /// <summary>
    /// When the rate was last updated
    /// </summary>
    public DateTime RateLastUpdated { get; set; }

    /// <summary>
    /// Request ID for tracking
    /// </summary>
    public required string RequestId { get; set; }
}
