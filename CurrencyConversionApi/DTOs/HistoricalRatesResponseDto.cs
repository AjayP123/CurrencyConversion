using System.Text.Json.Serialization;

namespace CurrencyConversionApi.DTOs;

/// <summary>
/// Response DTO for historical exchange rates
/// </summary>
public class HistoricalRatesResponseDto
{
    /// <summary>
    /// Base currency code
    /// </summary>
    [JsonPropertyName("base")]
    public required string BaseCurrency { get; set; }

    /// <summary>
    /// Historical date
    /// </summary>
    [JsonPropertyName("date")]
    public required DateTime Date { get; set; }

    /// <summary>
    /// Exchange rates from base currency on the specified date
    /// </summary>
    [JsonPropertyName("rates")]
    public required Dictionary<string, decimal> Rates { get; set; }

    /// <summary>
    /// Request correlation ID
    /// </summary>
    [JsonPropertyName("requestId")]
    public string? RequestId { get; set; }

    /// <summary>
    /// Data source
    /// </summary>
    [JsonPropertyName("source")]
    public string Source { get; set; } = "Frankfurter";

    /// <summary>
    /// Total count of rates
    /// </summary>
    [JsonPropertyName("count")]
    public int Count => Rates?.Count ?? 0;

    /// <summary>
    /// Indicates if the data is from a weekend/holiday (rates may be from previous business day)
    /// </summary>
    [JsonPropertyName("isWeekend")]
    public bool IsWeekend { get; set; }
}
