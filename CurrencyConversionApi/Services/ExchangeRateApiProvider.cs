using CurrencyConversionApi.Interfaces;
using CurrencyConversionApi.Models;
using Microsoft.Extensions.Options;
using CurrencyConversionApi.Configuration;
using System.Text.Json;
using System.Diagnostics.CodeAnalysis;

namespace CurrencyConversionApi.Services;

/// <summary>
/// ExchangeRate-API.com integration service
/// </summary>
[ExcludeFromCodeCoverage]
public class ExchangeRateApiProvider : IExchangeRateProvider
{
    private readonly HttpClient _httpClient;
    private readonly ExchangeRateConfig _config;
    private readonly ILogger<ExchangeRateApiProvider> _logger;
    
    // Excluded currencies as per requirements
    private static readonly HashSet<string> ExcludedCurrencies = new() { "TRY", "PLN", "THB", "MXN" };

    public string ProviderName => "ExchangeRateAPI";

    public ExchangeRateApiProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<ExchangeRateConfig> config,
        ILogger<ExchangeRateApiProvider> logger)
    {
        _httpClient = httpClientFactory.CreateClient("ExchangeRateApi");
        _config = config.Value;
        _logger = logger;
    }

    public async Task<ExchangeRate?> GetRateAsync(string fromCurrency, string toCurrency, CancellationToken cancellationToken = default)
    {
        if (IsExcludedCurrency(fromCurrency) || IsExcludedCurrency(toCurrency))
        {
            throw new ArgumentException($"Currency not supported: {fromCurrency} or {toCurrency}");
        }

        try
        {
            var response = await _httpClient.GetAsync($"latest/{fromCurrency}", cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var apiResponse = JsonSerializer.Deserialize<ExchangeRateApiResponse>(content, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            if (apiResponse?.Rates?.TryGetValue(toCurrency, out var rate) == true)
            {
                return new ExchangeRate
                {
                    FromCurrency = fromCurrency,
                    ToCurrency = toCurrency,
                    Rate = rate,
                    LastUpdated = DateTime.UtcNow,
                    Source = ProviderName
                };
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching rate from ExchangeRate API");
            throw;
        }
    }

    public async Task<IEnumerable<ExchangeRate>> GetLatestRatesAsync(string? baseCurrency = null, CancellationToken cancellationToken = default)
    {
        return await GetLatestRatesAsync(baseCurrency, null, cancellationToken);
    }

    public async Task<IEnumerable<ExchangeRate>> GetLatestRatesAsync(string? baseCurrency, List<string>? symbols, CancellationToken cancellationToken = default)
    {
        var baseCode = baseCurrency ?? "EUR";
        
        if (IsExcludedCurrency(baseCode))
        {
            throw new ArgumentException($"Base currency not supported: {baseCode}");
        }

        try
        {
            var response = await _httpClient.GetAsync($"latest/{baseCode}", cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var apiResponse = JsonSerializer.Deserialize<ExchangeRateApiResponse>(content, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            if (apiResponse?.Rates == null) return Enumerable.Empty<ExchangeRate>();

            var rates = new List<ExchangeRate>();
            var lastUpdated = DateTime.UtcNow;

            foreach (var rateKvp in apiResponse.Rates)
            {
                if (!IsExcludedCurrency(rateKvp.Key))
                {
                    // Apply symbols filter if provided
                    if (symbols == null || symbols.Contains(rateKvp.Key, StringComparer.OrdinalIgnoreCase))
                    {
                        rates.Add(new ExchangeRate
                        {
                            FromCurrency = baseCode,
                            ToCurrency = rateKvp.Key,
                            Rate = rateKvp.Value,
                            LastUpdated = lastUpdated,
                            Source = ProviderName
                        });
                    }
                }
            }

            return rates;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching latest rates from ExchangeRate API");
            throw;
        }
    }

    public async Task<IEnumerable<ExchangeRate>> GetHistoricalRatesAsync(DateTime date, string? baseCurrency = null, CancellationToken cancellationToken = default)
    {
        return await GetHistoricalRatesAsync(date, baseCurrency, null, cancellationToken);
    }

    public async Task<IEnumerable<ExchangeRate>> GetHistoricalRatesAsync(DateTime date, string? baseCurrency, List<string>? symbols, CancellationToken cancellationToken = default)
    {
        var baseCode = baseCurrency ?? "EUR";
        
        if (IsExcludedCurrency(baseCode))
        {
            throw new ArgumentException($"Base currency not supported: {baseCode}");
        }

        try
        {
            var dateStr = date.ToString("yyyy-MM-dd");
            var response = await _httpClient.GetAsync($"{dateStr}/{baseCode}", cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var apiResponse = JsonSerializer.Deserialize<ExchangeRateApiResponse>(content, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            if (apiResponse?.Rates == null) return Enumerable.Empty<ExchangeRate>();

            var rates = new List<ExchangeRate>();

            foreach (var rateKvp in apiResponse.Rates)
            {
                if (!IsExcludedCurrency(rateKvp.Key))
                {
                    // Apply symbols filter if provided
                    if (symbols == null || symbols.Contains(rateKvp.Key, StringComparer.OrdinalIgnoreCase))
                    {
                        rates.Add(new ExchangeRate
                        {
                            FromCurrency = baseCode,
                            ToCurrency = rateKvp.Key,
                            Rate = rateKvp.Value,
                            LastUpdated = date,
                            Source = ProviderName
                        });
                    }
                }
            }

            return rates;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching historical rates from ExchangeRate API");
            throw;
        }
    }

    public async Task<Dictionary<string, Dictionary<string, decimal>>> GetTimeSeriesRatesAsync(DateTime startDate, DateTime endDate, string? baseCurrency = null, List<string>? symbols = null, CancellationToken cancellationToken = default)
    {
        // ExchangeRate-API doesn't support time series, so we'll fetch individual historical rates
        var result = new Dictionary<string, Dictionary<string, decimal>>();
        var currentDate = startDate;
        
        while (currentDate <= endDate)
        {
            try
            {
                var rates = await GetHistoricalRatesAsync(currentDate, baseCurrency, symbols, cancellationToken);
                if (rates.Any())
                {
                    var dateStr = currentDate.ToString("yyyy-MM-dd");
                    result[dateStr] = rates.ToDictionary(r => r.ToCurrency, r => r.Rate);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get rates for date {Date}", currentDate.ToString("yyyy-MM-dd"));
            }
            
            currentDate = currentDate.AddDays(1);
        }

        return result;
    }

    public static bool IsExcludedCurrency(string currency)
    {
        return !string.IsNullOrEmpty(currency) && ExcludedCurrencies.Contains(currency.ToUpper());
    }
}

/// <summary>
/// ExchangeRate-API response model
/// </summary>
public class ExchangeRateApiResponse
{
    public string? Base { get; set; }
    public Dictionary<string, decimal>? Rates { get; set; }
}
