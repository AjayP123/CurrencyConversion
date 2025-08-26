using CurrencyConversionApi.Models;

namespace CurrencyConversionApi.Interfaces;

/// <summary>
/// Interface for exchange rate provider
/// </summary>
public interface IExchangeRateProvider
{
    /// <summary>
    /// Provider name
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Get latest exchange rates for a specific base currency
    /// </summary>
    Task<IEnumerable<ExchangeRate>> GetLatestRatesAsync(string? baseCurrency = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get latest exchange rates for a specific base currency with symbol filtering
    /// </summary>
    Task<IEnumerable<ExchangeRate>> GetLatestRatesAsync(string? baseCurrency, List<string>? symbols, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get historical exchange rates for a specific date and base currency
    /// </summary>
    Task<IEnumerable<ExchangeRate>> GetHistoricalRatesAsync(DateTime date, string? baseCurrency = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get historical exchange rates for a specific date and base currency with symbol filtering
    /// </summary>
    Task<IEnumerable<ExchangeRate>> GetHistoricalRatesAsync(DateTime date, string? baseCurrency, List<string>? symbols, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get time series exchange rates for a date range with symbol filtering
    /// </summary>
    Task<Dictionary<string, Dictionary<string, decimal>>> GetTimeSeriesRatesAsync(DateTime startDate, DateTime endDate, string? baseCurrency = null, List<string>? symbols = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get exchange rate for specific currency pair
    /// </summary>
    Task<ExchangeRate?> GetRateAsync(string fromCurrency, string toCurrency, CancellationToken cancellationToken = default);
}
