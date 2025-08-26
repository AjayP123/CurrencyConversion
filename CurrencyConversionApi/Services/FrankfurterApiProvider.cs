using CurrencyConversionApi.Interfaces;
using CurrencyConversionApi.DTOs;
using Microsoft.Extensions.Options;
using CurrencyConversionApi.Configuration;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CurrencyConversionApi.Services;

/// <summary>
/// Frankfurter API integration service
/// </summary>
public class FrankfurterApiProvider : IExchangeRateProvider
{
    private readonly HttpClient _httpClient;
    private readonly ExchangeRateConfig _config;
    private readonly ILogger<FrankfurterApiProvider> _logger;

    // Excluded currencies as per requirements
    private static readonly HashSet<string> ExcludedCurrencies = new() { "TRY", "PLN", "THB", "MXN" };

    public string ProviderName => "Frankfurter";

    public FrankfurterApiProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<ExchangeRateConfig> config,
        ILogger<FrankfurterApiProvider> logger)
    {
        _httpClient = httpClientFactory.CreateClient("FrankfurterApi");
        _config = config.Value;
        _logger = logger;
        
        // Log HttpClient configuration for debugging
        _logger.LogInformation("FrankfurterApiProvider initialized. BaseAddress: {BaseAddress}, Timeout: {Timeout}", 
            _httpClient.BaseAddress, _httpClient.Timeout);
        
        // HTTP client is now fully configured in ServiceCollectionExtensions
    }

    public async Task<Models.ExchangeRate?> GetRateAsync(string fromCurrency, string toCurrency, CancellationToken cancellationToken = default)
    {
        if (IsExcludedCurrency(fromCurrency) || IsExcludedCurrency(toCurrency))
        {
            throw new ArgumentException($"Currency not supported: {fromCurrency} or {toCurrency}");
        }

        try
        {
            var response = await _httpClient.GetAsync($"latest?from={fromCurrency}&to={toCurrency}", cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var frankfurterResponse = JsonSerializer.Deserialize<FrankfurterLatestResponse>(content, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (frankfurterResponse?.Rates?.TryGetValue(toCurrency, out var rate) == true)
            {
                return new Models.ExchangeRate
                {
                    FromCurrency = fromCurrency,
                    ToCurrency = toCurrency,
                    Rate = rate,
                    LastUpdated = DateTime.Parse(frankfurterResponse.Date),
                    Source = ProviderName
                };
            }

            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling Frankfurter API");
            throw;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout calling Frankfurter API");
            throw new TimeoutException("Frankfurter API request timed out", ex);
        }
    }

    public async Task<IEnumerable<Models.ExchangeRate>> GetLatestRatesAsync(string? baseCurrency = null, CancellationToken cancellationToken = default)
    {
        return await GetLatestRatesAsync(baseCurrency, null, cancellationToken);
    }

    public async Task<IEnumerable<Models.ExchangeRate>> GetLatestRatesAsync(string? baseCurrency, List<string>? symbols, CancellationToken cancellationToken = default)
    {
        var baseCode = baseCurrency ?? "EUR";
        
        if (IsExcludedCurrency(baseCode))
        {
            throw new ArgumentException($"Base currency not supported: {baseCode}");
        }

        try
        {
            var url = $"latest?from={baseCode}";
            
            // Add symbols filter if provided
            if (symbols?.Any() == true)
            {
                var filteredSymbols = symbols.Where(s => !IsExcludedCurrency(s)).ToList();
                if (filteredSymbols.Any())
                {
                    url += $"&to={string.Join(",", filteredSymbols)}";
                }
            }
            
            _logger.LogInformation("Making request to Frankfurter API. BaseAddress: {BaseAddress}, URL: {Url}, Full URL: {FullUrl}", 
                _httpClient.BaseAddress, url, $"{_httpClient.BaseAddress}{url}");
            
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var frankfurterResponse = JsonSerializer.Deserialize<FrankfurterLatestResponse>(content, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (frankfurterResponse == null) return Enumerable.Empty<Models.ExchangeRate>();

            var rates = new List<Models.ExchangeRate>();
            var lastUpdated = DateTime.Parse(frankfurterResponse.Date);

            foreach (var rateKvp in frankfurterResponse.Rates ?? new Dictionary<string, decimal>())
            {
                if (!IsExcludedCurrency(rateKvp.Key))
                {
                    rates.Add(new Models.ExchangeRate
                    {
                        FromCurrency = frankfurterResponse.Base,
                        ToCurrency = rateKvp.Key,
                        Rate = rateKvp.Value,
                        LastUpdated = lastUpdated,
                        Source = ProviderName
                    });
                }
            }

            return rates;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching latest rates from Frankfurter API");
            throw;
        }
    }

    public async Task<IEnumerable<Models.ExchangeRate>> GetHistoricalRatesAsync(DateTime date, string? baseCurrency = null, CancellationToken cancellationToken = default)
    {
        return await GetHistoricalRatesAsync(date, baseCurrency, null, cancellationToken);
    }

    public async Task<IEnumerable<Models.ExchangeRate>> GetHistoricalRatesAsync(DateTime date, string? baseCurrency, List<string>? symbols, CancellationToken cancellationToken = default)
    {
        var baseCode = baseCurrency ?? "EUR";
        
        if (IsExcludedCurrency(baseCode))
        {
            throw new ArgumentException($"Base currency not supported: {baseCode}");
        }

        try
        {
            var dateStr = date.ToString("yyyy-MM-dd");
            var url = $"{dateStr}?from={baseCode}";
            
            // Add symbols filter if provided
            if (symbols?.Any() == true)
            {
                var filteredSymbols = symbols.Where(s => !IsExcludedCurrency(s)).ToList();
                if (filteredSymbols.Any())
                {
                    url += $"&to={string.Join(",", filteredSymbols)}";
                }
            }
            
            _logger.LogInformation("Making request to Frankfurter API. BaseAddress: {BaseAddress}, URL: {Url}, Full URL: {FullUrl}", 
                _httpClient.BaseAddress, url, $"{_httpClient.BaseAddress}{url}");
            
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var frankfurterResponse = JsonSerializer.Deserialize<FrankfurterLatestResponse>(content, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (frankfurterResponse == null) return Enumerable.Empty<Models.ExchangeRate>();

            var rates = new List<Models.ExchangeRate>();
            var rateDate = DateTime.Parse(frankfurterResponse.Date);

            foreach (var rateKvp in frankfurterResponse.Rates ?? new Dictionary<string, decimal>())
            {
                if (!IsExcludedCurrency(rateKvp.Key))
                {
                    rates.Add(new Models.ExchangeRate
                    {
                        FromCurrency = frankfurterResponse.Base,
                        ToCurrency = rateKvp.Key,
                        Rate = rateKvp.Value,
                        LastUpdated = rateDate,
                        Source = ProviderName
                    });
                }
            }

            return rates;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching historical rates from Frankfurter API for date {Date}", date);
            throw;
        }
    }

    public async Task<Dictionary<string, Dictionary<string, decimal>>> GetTimeSeriesRatesAsync(DateTime startDate, DateTime endDate, string? baseCurrency = null, List<string>? symbols = null, CancellationToken cancellationToken = default)
    {
        var baseCode = baseCurrency ?? "EUR";
        
        if (IsExcludedCurrency(baseCode))
        {
            throw new ArgumentException($"Base currency not supported: {baseCode}");
        }

        try
        {
            var startDateStr = startDate.ToString("yyyy-MM-dd");
            var endDateStr = endDate.ToString("yyyy-MM-dd");
            var url = $"{startDateStr}..{endDateStr}?from={baseCode}";
            
            // Add symbols filter if provided
            if (symbols?.Any() == true)
            {
                var filteredSymbols = symbols.Where(s => !IsExcludedCurrency(s)).ToList();
                if (filteredSymbols.Any())
                {
                    url += $"&to={string.Join(",", filteredSymbols)}";
                }
            }
            
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var frankfurterResponse = JsonSerializer.Deserialize<FrankfurterTimeSeriesResponse>(content, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (frankfurterResponse?.Rates == null) return new Dictionary<string, Dictionary<string, decimal>>();

            // Filter out excluded currencies from each date's rates
            var filteredRates = new Dictionary<string, Dictionary<string, decimal>>();
            foreach (var dateRates in frankfurterResponse.Rates)
            {
                var cleanRates = new Dictionary<string, decimal>();
                foreach (var rate in dateRates.Value)
                {
                    if (!IsExcludedCurrency(rate.Key))
                    {
                        cleanRates[rate.Key] = rate.Value;
                    }
                }
                if (cleanRates.Any())
                {
                    filteredRates[dateRates.Key] = cleanRates;
                }
            }

            return filteredRates;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching time series rates from Frankfurter API for {StartDate} to {EndDate}", startDate, endDate);
            throw;
        }
    }

    public static bool IsExcludedCurrency(string currency)
    {
        return !string.IsNullOrEmpty(currency) && ExcludedCurrencies.Contains(currency.ToUpper());
    }
}

/// <summary>
/// Frankfurter API response model
/// </summary>
public class FrankfurterLatestResponse
{
    public decimal Amount { get; set; } = 1.0m;
    public required string Base { get; set; }
    public required string Date { get; set; }
    public required Dictionary<string, decimal> Rates { get; set; }
}

/// <summary>
/// Frankfurter time series response model
/// </summary>
public class FrankfurterTimeSeriesResponse
{
    public required string Base { get; set; }
    [JsonPropertyName("start_date")]
    public required string StartDate { get; set; }
    [JsonPropertyName("end_date")]
    public required string EndDate { get; set; }
    public required Dictionary<string, Dictionary<string, decimal>> Rates { get; set; }
}
