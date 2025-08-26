using CurrencyConversionApi.Interfaces;
using CurrencyConversionApi.DTOs;
using CurrencyConversionApi.Configuration;
using CurrencyConversionApi.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace CurrencyConversionApi.Services;

/// <summary>
/// Optimized cache service - Base currency rates only with smart TTL
/// </summary>
public class OptimizedCacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly SmartCacheConfig _smartConfig;
    private readonly ILogger<OptimizedCacheService> _logger;

    public OptimizedCacheService(
        IMemoryCache memoryCache,
        IOptions<SmartCacheConfig> smartConfig,
        ILogger<OptimizedCacheService> logger)
    {
        _memoryCache = memoryCache;
        _smartConfig = smartConfig.Value;
        _logger = logger;
    }

    /// <summary>
    /// Get cached base currency rates (e.g., EUR -> all currencies)
    /// </summary>
    public Task<IEnumerable<ExchangeRate>?> GetLatestRatesAsync(string baseCurrency)
    {
        var key = $"latest_rates_{baseCurrency}";
        
        if (_memoryCache.TryGetValue(key, out var cached) && cached is IEnumerable<ExchangeRate> rates)
        {
            _logger.LogDebug("Cache hit for latest rates: {BaseCurrency}", baseCurrency);
            return Task.FromResult<IEnumerable<ExchangeRate>?>(rates);
        }

        _logger.LogDebug("Cache miss for latest rates: {BaseCurrency}", baseCurrency);
        return Task.FromResult<IEnumerable<ExchangeRate>?>(null);
    }

    /// <summary>
    /// Cache base currency rates with smart TTL
    /// </summary>
    public Task SetLatestRatesAsync(string baseCurrency, IEnumerable<ExchangeRate> rates, TimeSpan? expiry = null)
    {
        var key = $"latest_rates_{baseCurrency}";
        var ttl = expiry ?? _smartConfig.GetOptimalTTL();
        
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl,
            Priority = CacheItemPriority.High
        };

        _memoryCache.Set(key, rates, options);
        
        _logger.LogInformation("Cached latest rates for {BaseCurrency} (TTL: {TTL}ms)", 
            baseCurrency, ttl.TotalMilliseconds);
        
        return Task.CompletedTask;
    }

    /// <summary>
    /// Get exchange rate using cached base currency data (smart conversion)
    /// </summary>
    public async Task<ExchangeRate?> GetExchangeRateAsync(string fromCurrency, string toCurrency)
    {
        var rate = await GetConversionRateAsync(fromCurrency, toCurrency);
        if (rate.HasValue)
        {
            return new ExchangeRate
            {
                FromCurrency = fromCurrency,
                ToCurrency = toCurrency,
                Rate = rate.Value,
                LastUpdated = DateTime.UtcNow,
                Source = "Cache"
            };
        }
        return null;
    }

    /// <summary>
    /// Calculate conversion rate using cached base currency data
    /// </summary>
    private async Task<decimal?> GetConversionRateAsync(string fromCurrency, string toCurrency)
    {
        // Direct conversion (same currency)
        if (fromCurrency == toCurrency) return 1.0m;

        // Try EUR as base first (most common)
        var eurRates = await GetLatestRatesAsync("EUR");
        if (eurRates != null)
        {
            var ratesList = eurRates.ToList();

            // EUR -> toCurrency (direct)
            if (fromCurrency == "EUR")
            {
                var directRate = ratesList.FirstOrDefault(r => r.ToCurrency == toCurrency);
                if (directRate != null) return directRate.Rate;
            }

            // fromCurrency -> EUR (inverse)
            if (toCurrency == "EUR")
            {
                var inverseRate = ratesList.FirstOrDefault(r => r.ToCurrency == fromCurrency);
                if (inverseRate != null) return 1.0m / inverseRate.Rate;
            }

            // fromCurrency -> EUR -> toCurrency (cross rate)
            var fromRate = ratesList.FirstOrDefault(r => r.ToCurrency == fromCurrency);
            var toRate = ratesList.FirstOrDefault(r => r.ToCurrency == toCurrency);
            
            if (fromRate != null && toRate != null)
            {
                return toRate.Rate / fromRate.Rate;
            }
        }

        // Try other base currencies if needed (USD, GBP, etc.)
        _logger.LogDebug("No cached conversion rate found for {From} -> {To}", fromCurrency, toCurrency);
        return null;
    }
}
