using CurrencyConversionApi.Interfaces;
using CurrencyConversionApi.Models;
using CurrencyConversionApi.Utilities;

namespace CurrencyConversionApi.Services;

/// <summary>
/// Core currency conversion service
/// </summary>
public class CurrencyConversionService : ICurrencyConversionService
{
    private readonly IExchangeRateProviderFactory _providerFactory;
    private readonly ICacheService _cacheService;
    private readonly ILogger<CurrencyConversionService> _logger;

    // Common currencies with their metadata (excluding TRY, PLN, THB, MXN per requirements)
    private static readonly Dictionary<string, Currency> _commonCurrencies = new()
    {
        ["USD"] = new() { Code = "USD", Name = "US Dollar", Symbol = "$", DecimalPlaces = 2 },
        ["EUR"] = new() { Code = "EUR", Name = "Euro", Symbol = "€", DecimalPlaces = 2 },
        ["GBP"] = new() { Code = "GBP", Name = "British Pound", Symbol = "£", DecimalPlaces = 2 },
        ["JPY"] = new() { Code = "JPY", Name = "Japanese Yen", Symbol = "¥", DecimalPlaces = 0 },
        ["CAD"] = new() { Code = "CAD", Name = "Canadian Dollar", Symbol = "C$", DecimalPlaces = 2 },
        ["AUD"] = new() { Code = "AUD", Name = "Australian Dollar", Symbol = "A$", DecimalPlaces = 2 },
        ["CHF"] = new() { Code = "CHF", Name = "Swiss Franc", Symbol = "Fr", DecimalPlaces = 2 },
        ["CNY"] = new() { Code = "CNY", Name = "Chinese Yuan", Symbol = "¥", DecimalPlaces = 2 },
        ["INR"] = new() { Code = "INR", Name = "Indian Rupee", Symbol = "₹", DecimalPlaces = 2 },
        ["KRW"] = new() { Code = "KRW", Name = "South Korean Won", Symbol = "₩", DecimalPlaces = 0 },
        ["BRL"] = new() { Code = "BRL", Name = "Brazilian Real", Symbol = "R$", DecimalPlaces = 2 },
        ["RUB"] = new() { Code = "RUB", Name = "Russian Ruble", Symbol = "₽", DecimalPlaces = 2 },
        ["SGD"] = new() { Code = "SGD", Name = "Singapore Dollar", Symbol = "S$", DecimalPlaces = 2 },
        ["HKD"] = new() { Code = "HKD", Name = "Hong Kong Dollar", Symbol = "HK$", DecimalPlaces = 2 }
    };

    public CurrencyConversionService(
        IExchangeRateProviderFactory providerFactory,
        ICacheService cacheService,
        ILogger<CurrencyConversionService> logger)
    {
        _providerFactory = providerFactory;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<ConversionResult> ConvertAsync(decimal amount, string fromCurrency, string toCurrency, CancellationToken cancellationToken = default)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero", nameof(amount));

        fromCurrency = ValidateCurrency(fromCurrency, nameof(fromCurrency));
        toCurrency = ValidateCurrency(toCurrency, nameof(toCurrency));

        _logger.LogInformation("Converting {Amount} from {From} to {To}", amount, fromCurrency, toCurrency);

        // Same currency conversion
        if (fromCurrency == toCurrency)
        {
            return new ConversionResult
            {
                Amount = amount,
                FromCurrency = fromCurrency,
                ToCurrency = toCurrency,
                ConvertedAmount = amount,
                ExchangeRate = 1.0m,
                RateLastUpdated = DateTime.UtcNow,
                RateSource = "Direct"
            };
        }

        var exchangeRate = await GetExchangeRateAsync(fromCurrency, toCurrency, cancellationToken);
        if (exchangeRate == null)
        {
            throw new InvalidOperationException($"Unable to get exchange rate from {fromCurrency} to {toCurrency}");
        }

        var convertedAmount = Math.Round(amount * exchangeRate.Rate, GetDecimalPlaces(toCurrency));

        return new ConversionResult
        {
            Amount = amount,
            FromCurrency = fromCurrency,
            ToCurrency = toCurrency,
            ConvertedAmount = convertedAmount,
            ExchangeRate = exchangeRate.Rate,
            RateLastUpdated = exchangeRate.LastUpdated,
            RateSource = exchangeRate.Source
        };
    }

    public async Task<IEnumerable<ExchangeRate>> GetLatestRatesAsync(string? baseCurrency = null, CancellationToken cancellationToken = default)
    {
        return await GetLatestRatesAsync(baseCurrency, null, cancellationToken);
    }

    public async Task<IEnumerable<ExchangeRate>> GetLatestRatesAsync(string? baseCurrency, List<string>? symbols, CancellationToken cancellationToken = default)
    {
        var baseCode = baseCurrency ?? "EUR";
        baseCode = ValidateCurrency(baseCode, nameof(baseCurrency));

        _logger.LogInformation("Getting latest rates for base currency {BaseCurrency} with symbols {Symbols}", baseCode, symbols);

        // Try cache first - always cache complete rates set, filter on return
        var cachedRates = await _cacheService.GetLatestRatesAsync(baseCode);
        if (cachedRates != null)
        {
            _logger.LogDebug("Using cached latest rates for {BaseCurrency}", baseCode);
            var cachedList = cachedRates.ToList();
            
            // If symbols filter is specified, filter the cached results
            if (symbols?.Any() == true)
            {
                cachedList = cachedList.Where(r => symbols.Contains(r.ToCurrency, StringComparer.OrdinalIgnoreCase)).ToList();
            }
            
            return cachedList;
        }

        // Try active provider - always fetch complete rates set
        var activeProvider = _providerFactory.GetActiveProvider();
        try
        {
            // Fetch ALL rates (no symbols filtering at provider level) to cache complete set
            var allRates = await activeProvider.GetLatestRatesAsync(baseCode, null, cancellationToken);
            if (allRates?.Any() == true)
            {
                _logger.LogInformation("Got {Count} latest rates from provider {Provider}", allRates.Count(), activeProvider.ProviderName);
                
                // Cache the complete rates set
                await _cacheService.SetLatestRatesAsync(baseCode, allRates, null);
                
                // Filter results based on symbols before returning
                if (symbols?.Any() == true)
                {
                    var filteredRates = allRates.Where(r => symbols.Contains(r.ToCurrency, StringComparer.OrdinalIgnoreCase)).ToList();
                    return filteredRates;
                }
                
                return allRates;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting latest rates from provider {Provider}", activeProvider.ProviderName);
        }

        _logger.LogWarning("No providers could provide latest rates for {BaseCurrency}", baseCode);
        return Enumerable.Empty<ExchangeRate>();
    }

    public async Task<IEnumerable<ExchangeRate>> GetHistoricalRatesAsync(DateTime date, string? baseCurrency = null, CancellationToken cancellationToken = default)
    {
        return await GetHistoricalRatesAsync(date, baseCurrency, null, cancellationToken);
    }

    public async Task<IEnumerable<ExchangeRate>> GetHistoricalRatesAsync(DateTime date, string? baseCurrency, List<string>? symbols, CancellationToken cancellationToken = default)
    {
        var baseCode = baseCurrency ?? "EUR";
        baseCode = ValidateCurrency(baseCode, nameof(baseCurrency));

        if (date.Date > DateTime.Today)
            throw new ArgumentException("Historical date cannot be in the future", nameof(date));

        _logger.LogInformation("Getting historical rates for {Date} with base currency {BaseCurrency} and symbols {Symbols}", 
            date.ToString("yyyy-MM-dd"), baseCode, symbols);

        // No caching for historical data as per our previous discussion
        var activeProvider = _providerFactory.GetActiveProvider();
        try
        {
            // For historical rates, we can fetch with symbols directly since we're not caching
            var rates = await activeProvider.GetHistoricalRatesAsync(date, baseCode, symbols, cancellationToken);
            if (rates?.Any() == true)
            {
                _logger.LogInformation("Got {Count} historical rates from provider {Provider} for {Date}", 
                    rates.Count(), activeProvider.ProviderName, date.ToString("yyyy-MM-dd"));
                return rates;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting historical rates from provider {Provider} for {Date}", 
                activeProvider.ProviderName, date.ToString("yyyy-MM-dd"));
        }

        _logger.LogWarning("No providers could provide historical rates for {BaseCurrency} on {Date}", baseCode, date.ToString("yyyy-MM-dd"));
        return Enumerable.Empty<ExchangeRate>();
    }

    public async Task<Dictionary<string, Dictionary<string, decimal>>> GetTimeSeriesRatesAsync(DateTime startDate, DateTime endDate, string? baseCurrency = null, List<string>? symbols = null, CancellationToken cancellationToken = default)
    {
        var baseCode = baseCurrency ?? "EUR";
        baseCode = ValidateCurrency(baseCode, nameof(baseCurrency));

        if (startDate.Date > DateTime.Today)
            throw new ArgumentException("Start date cannot be in the future", nameof(startDate));
        if (endDate.Date > DateTime.Today)
            throw new ArgumentException("End date cannot be in the future", nameof(endDate));
        if (startDate > endDate)
            throw new ArgumentException("Start date cannot be after end date", nameof(startDate));

        _logger.LogInformation("Getting time series rates from {StartDate} to {EndDate} with base currency {BaseCurrency} and symbols {Symbols}", 
            startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"), baseCode, symbols);

        // No caching for time series data due to complexity and size
        var activeProvider = _providerFactory.GetActiveProvider();
        try
        {
            var timeSeriesData = await activeProvider.GetTimeSeriesRatesAsync(startDate, endDate, baseCode, symbols, cancellationToken);
            if (timeSeriesData?.Any() == true)
            {
                _logger.LogInformation("Got time series data with {Count} dates from provider {Provider}", 
                    timeSeriesData.Count, activeProvider.ProviderName);
                return timeSeriesData;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting time series rates from provider {Provider} for {StartDate} to {EndDate}", 
                activeProvider.ProviderName, startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"));
        }

        _logger.LogWarning("No providers could provide time series rates for {BaseCurrency} from {StartDate} to {EndDate}", 
            baseCode, startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"));
        return new Dictionary<string, Dictionary<string, decimal>>();
    }

    public async Task<ExchangeRate?> GetExchangeRateAsync(string fromCurrency, string toCurrency, CancellationToken cancellationToken = default)
    {
        fromCurrency = ValidateCurrency(fromCurrency, nameof(fromCurrency));
        toCurrency = ValidateCurrency(toCurrency, nameof(toCurrency));

        if (fromCurrency == toCurrency)
        {
            return new ExchangeRate
            {
                FromCurrency = fromCurrency,
                ToCurrency = toCurrency,
                Rate = 1.0m,
                Source = "Direct"
            };
        }

        // Try cache first
        var cachedRate = await _cacheService.GetExchangeRateAsync(fromCurrency, toCurrency);
        if (cachedRate != null)
        {
            _logger.LogDebug("Using cached exchange rate for {From} to {To}", fromCurrency, toCurrency);
            return cachedRate;
        }

        // Try active provider
        var activeProvider = _providerFactory.GetActiveProvider();
        try
        {
            var rate = await activeProvider.GetRateAsync(fromCurrency, toCurrency, cancellationToken);
            if (rate != null)
            {
                _logger.LogInformation("Got exchange rate from provider {Provider}", activeProvider.ProviderName);
                
                // Note: Individual rate caching is handled by base currency caching strategy
                return rate;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting rate from provider {Provider}", activeProvider.ProviderName);
        }

        _logger.LogWarning("No providers could provide exchange rate for {From} to {To}", fromCurrency, toCurrency);
        return null;
    }

    public async Task<IEnumerable<Currency>> GetSupportedCurrenciesAsync(CancellationToken cancellationToken = default)
    {
        // Return static currencies - no caching needed as this is static content
        var currencies = _commonCurrencies.Values.ToList();
        
        _logger.LogInformation("Returning {Count} supported currencies", currencies.Count);
        return await Task.FromResult(currencies);
    }

    public async Task<bool> IsCurrencySupportedAsync(string currencyCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(currencyCode) || currencyCode.Length != 3)
            return false;

        currencyCode = currencyCode.ToUpper();

        var currencies = await GetSupportedCurrenciesAsync(cancellationToken);
        return currencies.Any(c => c.Code == currencyCode);
    }

    private static string ValidateCurrency(string currency, string paramName)
    {
        return CurrencyValidationHelper.ValidateAndNormalizeCurrency(currency, paramName);
    }

    private static int GetDecimalPlaces(string currencyCode)
    {
        return _commonCurrencies.TryGetValue(currencyCode, out var currency) 
            ? currency.DecimalPlaces 
            : 2;
    }
}
