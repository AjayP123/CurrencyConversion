using System.Text.Json.Serialization;

namespace CurrencyConversionApi.DTOs;

/// <summary>
/// Response DTO for latest exchange rates
/// </summary>
public class LatestRatesResponseDto
{
    /// <summary>
    /// Base currency code
    /// </summary>
    [JsonPropertyName("base")]
    public required string BaseCurrency { get; set; }

    /// <summary>
    /// Date of the rates
    /// </summary>
    [JsonPropertyName("date")]
    public required DateTime Date { get; set; }

    /// <summary>
    /// Exchange rates from base currency
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
}
