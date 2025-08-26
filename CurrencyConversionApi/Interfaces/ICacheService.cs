using CurrencyConversionApi.Models;

namespace CurrencyConversionApi.Interfaces;

/// <summary>
/// Interface for caching exchange rates - focused on base currency strategy only
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Get cached exchange rate
    /// </summary>
    Task<ExchangeRate?> GetExchangeRateAsync(string fromCurrency, string toCurrency);

    /// <summary>
    /// Get cached latest rates for base currency
    /// </summary>
    Task<IEnumerable<ExchangeRate>?> GetLatestRatesAsync(string baseCurrency);

    /// <summary>
    /// Cache latest rates for base currency with smart TTL
    /// </summary>
    Task SetLatestRatesAsync(string baseCurrency, IEnumerable<ExchangeRate> rates, TimeSpan? expiry = null);
}
