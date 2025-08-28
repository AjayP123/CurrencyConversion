using CurrencyConversionApi.Interfaces;
using CurrencyConversionApi.Models;
using Microsoft.Extensions.Options;
using CurrencyConversionApi.Configuration;
using System.Text.Json;
using System.Diagnostics.CodeAnalysis;

namespace CurrencyConversionApi.Services;

/// <summary>
/// CurrencyAPI.com integration service
/// </summary>
[ExcludeFromCodeCoverage]
public class CurrencyApiProvider : IExchangeRateProvider
{
    private readonly HttpClient _httpClient;
    private readonly ExchangeRateConfig _config;
    private readonly ILogger<CurrencyApiProvider> _logger;
    
    // Excluded currencies as per requirements
    private static readonly HashSet<string> ExcludedCurrencies = new() { "TRY", "PLN", "THB", "MXN" };

    public string ProviderName => "CurrencyAPI";

    public CurrencyApiProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<ExchangeRateConfig> config,
        ILogger<CurrencyApiProvider> logger)
    {
        _httpClient = httpClientFactory.CreateClient("CurrencyApi");
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
            var apiKey = GetApiKey();
            var response = await _httpClient.GetAsync($"latest?apikey={apiKey}&base_currency={fromCurrency}&currencies={toCurrency}", cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var apiResponse = JsonSerializer.Deserialize<CurrencyApiResponse>(content, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            if (apiResponse?.Data?.TryGetValue(toCurrency, out var rateInfo) == true)
            {
                return new ExchangeRate
                {
                    FromCurrency = fromCurrency,
                    ToCurrency = toCurrency,
                    Rate = rateInfo.Value,
                    LastUpdated = DateTime.UtcNow,
                    Source = ProviderName
                };
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching rate from Currency API");
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
            var apiKey = GetApiKey();
            var currenciesParam = symbols?.Any() == true ? string.Join(",", symbols) : "";
            var url = $"latest?apikey={apiKey}&base_currency={baseCode}";
            if (!string.IsNullOrEmpty(currenciesParam))
            {
                url += $"&currencies={currenciesParam}";
            }

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var apiResponse = JsonSerializer.Deserialize<CurrencyApiResponse>(content, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            if (apiResponse?.Data == null) return Enumerable.Empty<ExchangeRate>();

            var rates = new List<ExchangeRate>();
            var lastUpdated = DateTime.UtcNow;

            foreach (var rateKvp in apiResponse.Data)
            {
                if (!IsExcludedCurrency(rateKvp.Key))
                {
                    rates.Add(new ExchangeRate
                    {
                        FromCurrency = baseCode,
                        ToCurrency = rateKvp.Key,
                        Rate = rateKvp.Value.Value,
                        LastUpdated = lastUpdated,
                        Source = ProviderName
                    });
                }
            }

            return rates;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching latest rates from Currency API");
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
            var apiKey = GetApiKey();
            var dateStr = date.ToString("yyyy-MM-dd");
            var currenciesParam = symbols?.Any() == true ? string.Join(",", symbols) : "";
            var url = $"historical?apikey={apiKey}&date={dateStr}&base_currency={baseCode}";
            if (!string.IsNullOrEmpty(currenciesParam))
            {
                url += $"&currencies={currenciesParam}";
            }

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var apiResponse = JsonSerializer.Deserialize<CurrencyApiResponse>(content, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            if (apiResponse?.Data == null) return Enumerable.Empty<ExchangeRate>();

            var rates = new List<ExchangeRate>();

            foreach (var rateKvp in apiResponse.Data)
            {
                if (!IsExcludedCurrency(rateKvp.Key))
                {
                    rates.Add(new ExchangeRate
                    {
                        FromCurrency = baseCode,
                        ToCurrency = rateKvp.Key,
                        Rate = rateKvp.Value.Value,
                        LastUpdated = date,
                        Source = ProviderName
                    });
                }
            }

            return rates;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching historical rates from Currency API");
            throw;
        }
    }

    public async Task<Dictionary<string, Dictionary<string, decimal>>> GetTimeSeriesRatesAsync(DateTime startDate, DateTime endDate, string? baseCurrency = null, List<string>? symbols = null, CancellationToken cancellationToken = default)
    {
        // CurrencyAPI doesn't support time series directly, so we'll fetch individual historical rates
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

    private string GetApiKey()
    {
        var providerConfig = _config.Providers?.GetValueOrDefault("CurrencyAPI");
        if (string.IsNullOrEmpty(providerConfig?.ApiKey))
        {
            throw new InvalidOperationException("CurrencyAPI requires an API key. Please configure it in appsettings.json or user secrets.");
        }
        return providerConfig.ApiKey;
    }

    public static bool IsExcludedCurrency(string currency)
    {
        return !string.IsNullOrEmpty(currency) && ExcludedCurrencies.Contains(currency.ToUpper());
    }
}

/// <summary>
/// CurrencyAPI response model
/// </summary>
public class CurrencyApiResponse
{
    public Dictionary<string, CurrencyApiRateInfo>? Data { get; set; }
}

/// <summary>
/// CurrencyAPI rate info model
/// </summary>
public class CurrencyApiRateInfo
{
    public decimal Value { get; set; }
    public string? Code { get; set; }
}
