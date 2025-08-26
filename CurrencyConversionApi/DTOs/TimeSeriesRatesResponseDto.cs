using System.Text.Json.Serialization;

namespace CurrencyConversionApi.DTOs;

/// <summary>
/// Response DTO for time series exchange rates
/// </summary>
public class TimeSeriesRatesResponseDto
{
    /// <summary>
    /// Base currency code
    /// </summary>
    [JsonPropertyName("base")]
    public required string BaseCurrency { get; set; }

    /// <summary>
    /// Start date of the time series
    /// </summary>
    [JsonPropertyName("start_date")]
    public required DateTime StartDate { get; set; }

    /// <summary>
    /// End date of the time series
    /// </summary>
    [JsonPropertyName("end_date")]
    public required DateTime EndDate { get; set; }

    /// <summary>
    /// Time series exchange rates organized by date
    /// Key: Date in YYYY-MM-DD format
    /// Value: Dictionary of currency code to exchange rate
    /// </summary>
    [JsonPropertyName("rates")]
    public required Dictionary<string, Dictionary<string, decimal>> Rates { get; set; }

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
    /// Total count of dates in the time series
    /// </summary>
    [JsonPropertyName("count")]
    public int Count => Rates?.Count ?? 0;
}
