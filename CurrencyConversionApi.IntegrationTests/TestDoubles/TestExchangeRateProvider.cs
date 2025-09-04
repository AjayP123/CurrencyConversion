using CurrencyConversionApi.Interfaces;
using CurrencyConversionApi.Models;

namespace CurrencyConversionApi.IntegrationTests.TestDoubles;

public class TestExchangeRateProvider : IExchangeRateProvider
{
    public string ProviderName => "Mock";

    private readonly Dictionary<string, Dictionary<string, decimal>> _rates;

    public TestExchangeRateProvider()
    {
        // Initialize with predictable test data
        _rates = new Dictionary<string, Dictionary<string, decimal>>
        {
            ["USD"] = new Dictionary<string, decimal>
            {
                ["EUR"] = 0.85m,
                ["GBP"] = 0.73m,
                ["JPY"] = 110.0m,
                ["CAD"] = 1.25m,
                ["AUD"] = 1.35m,
                ["CHF"] = 0.92m
            },
            ["EUR"] = new Dictionary<string, decimal>
            {
                ["USD"] = 1.18m,
                ["GBP"] = 0.86m,
                ["JPY"] = 129.4m,
                ["CAD"] = 1.47m,
                ["AUD"] = 1.59m,
                ["CHF"] = 1.08m
            },
            ["GBP"] = new Dictionary<string, decimal>
            {
                ["USD"] = 1.37m,
                ["EUR"] = 1.16m,
                ["JPY"] = 150.7m,
                ["CAD"] = 1.71m,
                ["AUD"] = 1.85m,
                ["CHF"] = 1.26m
            }
        };
    }

    public async Task<ExchangeRate?> GetRateAsync(string fromCurrency, string toCurrency, CancellationToken cancellationToken = default)
    {
        await Task.Delay(10, cancellationToken); // Simulate network delay

        if (_rates.TryGetValue(fromCurrency, out var fromRates) &&
            fromRates.TryGetValue(toCurrency, out var rate))
        {
            return new ExchangeRate
            {
                FromCurrency = fromCurrency,
                ToCurrency = toCurrency,
                Rate = rate,
                LastUpdated = DateTime.UtcNow,
                Source = "TestProvider"
            };
        }

        return null;
    }

    public async Task<IEnumerable<ExchangeRate>> GetLatestRatesAsync(string? baseCurrency = null, CancellationToken cancellationToken = default)
    {
        await Task.Delay(10, cancellationToken); // Simulate network delay

        var results = new List<ExchangeRate>();
        baseCurrency ??= "USD";

        if (_rates.TryGetValue(baseCurrency, out var fromRates))
        {
            foreach (var kvp in fromRates)
            {
                results.Add(new ExchangeRate
                {
                    FromCurrency = baseCurrency,
                    ToCurrency = kvp.Key,
                    Rate = kvp.Value,
                    LastUpdated = DateTime.UtcNow,
                    Source = "TestProvider"
                });
            }
        }

        return results;
    }

    public async Task<IEnumerable<ExchangeRate>> GetLatestRatesAsync(string? baseCurrency, List<string>? symbols, CancellationToken cancellationToken = default)
    {
        await Task.Delay(10, cancellationToken); // Simulate network delay

        var results = new List<ExchangeRate>();
        baseCurrency ??= "USD";
        symbols ??= new List<string>();

        if (_rates.TryGetValue(baseCurrency, out var fromRates))
        {
            foreach (var symbol in symbols)
            {
                if (fromRates.TryGetValue(symbol, out var rate))
                {
                    results.Add(new ExchangeRate
                    {
                        FromCurrency = baseCurrency,
                        ToCurrency = symbol,
                        Rate = rate,
                        LastUpdated = DateTime.UtcNow,
                        Source = "TestProvider"
                    });
                }
            }
        }

        return results;
    }

    public async Task<IEnumerable<ExchangeRate>> GetHistoricalRatesAsync(DateTime date, string? baseCurrency = null, CancellationToken cancellationToken = default)
    {
        await Task.Delay(10, cancellationToken); // Simulate network delay

        var results = new List<ExchangeRate>();
        baseCurrency ??= "USD";

        if (_rates.TryGetValue(baseCurrency, out var fromRates))
        {
            foreach (var kvp in fromRates)
            {
                // Add some variation to the base rate for historical data
                var variation = (date.Day % 10 - 5) * 0.001m;
                var historicalRate = kvp.Value + variation;

                results.Add(new ExchangeRate
                {
                    FromCurrency = baseCurrency,
                    ToCurrency = kvp.Key,
                    Rate = Math.Max(0.0001m, historicalRate),
                    LastUpdated = date,
                    Source = "TestProvider"
                });
            }
        }

        return results;
    }

    public async Task<IEnumerable<ExchangeRate>> GetHistoricalRatesAsync(DateTime date, string? baseCurrency, List<string>? symbols, CancellationToken cancellationToken = default)
    {
        await Task.Delay(10, cancellationToken); // Simulate network delay

        var results = new List<ExchangeRate>();
        baseCurrency ??= "USD";
        symbols ??= new List<string>();

        if (_rates.TryGetValue(baseCurrency, out var fromRates))
        {
            foreach (var symbol in symbols)
            {
                if (fromRates.TryGetValue(symbol, out var baseRate))
                {
                    // Add some variation to the base rate for historical data
                    var variation = (date.Day % 10 - 5) * 0.001m;
                    var historicalRate = baseRate + variation;

                    results.Add(new ExchangeRate
                    {
                        FromCurrency = baseCurrency,
                        ToCurrency = symbol,
                        Rate = Math.Max(0.0001m, historicalRate),
                        LastUpdated = date,
                        Source = "TestProvider"
                    });
                }
            }
        }

        return results;
    }

    public async Task<Dictionary<string, Dictionary<string, decimal>>> GetTimeSeriesRatesAsync(DateTime startDate, DateTime endDate, string? baseCurrency = null, List<string>? symbols = null, CancellationToken cancellationToken = default)
    {
        await Task.Delay(10, cancellationToken); // Simulate network delay

        var results = new Dictionary<string, Dictionary<string, decimal>>();
        baseCurrency ??= "USD";
        symbols ??= new List<string> { "EUR", "GBP", "JPY" };

        if (_rates.TryGetValue(baseCurrency, out var fromRates))
        {
            var currentDate = startDate;
            while (currentDate <= endDate)
            {
                var dateStr = currentDate.ToString("yyyy-MM-dd");
                results[dateStr] = new Dictionary<string, decimal>();

                foreach (var symbol in symbols)
                {
                    if (fromRates.TryGetValue(symbol, out var baseRate))
                    {
                        // Add some variation to the base rate for time series data
                        var variation = (currentDate.Day % 10 - 5) * 0.001m;
                        var timeSeriesRate = baseRate + variation;
                        results[dateStr][symbol] = Math.Max(0.0001m, timeSeriesRate);
                    }
                }

                currentDate = currentDate.AddDays(1);
            }
        }

        return results;
    }
}
