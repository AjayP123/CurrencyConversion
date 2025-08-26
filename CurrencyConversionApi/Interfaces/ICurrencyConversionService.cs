using CurrencyConversionApi.Models;

namespace CurrencyConversionApi.Interfaces;

/// <summary>
/// Interface for currency conversion service
/// </summary>
public interface ICurrencyConversionService
{
    /// <summary>
    /// Convert amount from one currency to another
    /// </summary>
    Task<ConversionResult> ConvertAsync(decimal amount, string fromCurrency, string toCurrency, CancellationToken cancellationToken = default);

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
    /// Get time series exchange rates for a date range with optional symbol filtering
    /// </summary>
    Task<Dictionary<string, Dictionary<string, decimal>>> GetTimeSeriesRatesAsync(DateTime startDate, DateTime endDate, string? baseCurrency = null, List<string>? symbols = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get current exchange rate between two currencies
    /// </summary>
    Task<ExchangeRate?> GetExchangeRateAsync(string fromCurrency, string toCurrency, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all supported currencies
    /// </summary>
    Task<IEnumerable<Currency>> GetSupportedCurrenciesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if currency is supported
    /// </summary>
    Task<bool> IsCurrencySupportedAsync(string currencyCode, CancellationToken cancellationToken = default);
}
