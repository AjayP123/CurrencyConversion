using CurrencyConversionApi.Interfaces;
using CurrencyConversionApi.Configuration;
using Microsoft.Extensions.Options;

namespace CurrencyConversionApi.Services;

/// <summary>
/// Factory for creating exchange rate providers based on configuration
/// </summary>
public interface IExchangeRateProviderFactory
{
    /// <summary>
    /// Get the active provider based on configuration
    /// </summary>
    IExchangeRateProvider GetActiveProvider();
    
    /// <summary>
    /// Get all available providers
    /// </summary>
    IEnumerable<IExchangeRateProvider> GetAllProviders();
}

/// <summary>
/// Implementation of exchange rate provider factory
/// </summary>
public class ExchangeRateProviderFactory : IExchangeRateProviderFactory
{
    private readonly ExchangeRateConfig _config;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ExchangeRateProviderFactory> _logger;

    public ExchangeRateProviderFactory(
        IOptions<ExchangeRateConfig> config,
        IServiceProvider serviceProvider,
        ILogger<ExchangeRateProviderFactory> logger)
    {
        _config = config.Value;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public IExchangeRateProvider GetActiveProvider()
    {
        var activeProviderName = _config.ActiveProvider;
        _logger.LogInformation("Getting active provider: {ProviderName}", activeProviderName);

        return activeProviderName.ToLower() switch
        {
            "frankfurter" => _serviceProvider.GetRequiredService<FrankfurterApiProvider>(),
            "exchangerateapi" => _serviceProvider.GetRequiredService<ExchangeRateApiProvider>(),
            "currencyapi" => _serviceProvider.GetRequiredService<CurrencyApiProvider>(),
            _ => throw new InvalidOperationException($"Unknown provider: {activeProviderName}")
        };
    }

    public IEnumerable<IExchangeRateProvider> GetAllProviders()
    {
        var providers = new List<IExchangeRateProvider>
        {
            // Get all registered providers
            _serviceProvider.GetRequiredService<FrankfurterApiProvider>(),
            _serviceProvider.GetRequiredService<ExchangeRateApiProvider>(),
            _serviceProvider.GetRequiredService<CurrencyApiProvider>()
        };
        
        return providers.Where(p => IsProviderEnabled(p.ProviderName));
    }

    private bool IsProviderEnabled(string providerName)
    {
        var providerConfig = _config.Providers?.GetValueOrDefault(providerName);
        return providerConfig?.Enabled ?? false;
    }
}
