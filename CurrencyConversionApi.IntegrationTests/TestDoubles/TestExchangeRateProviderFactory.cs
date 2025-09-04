using CurrencyConversionApi.Interfaces;
using CurrencyConversionApi.Services;
using CurrencyConversionApi.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using CurrencyConversionApi.Models;

namespace CurrencyConversionApi.IntegrationTests.TestDoubles;

public class TestExchangeRateProviderFactory : IExchangeRateProviderFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ExchangeRateConfig _config;
    private readonly ILogger<TestExchangeRateProviderFactory> _logger;

    public TestExchangeRateProviderFactory(
        IServiceProvider serviceProvider,
        IOptions<ExchangeRateConfig> config,
        ILogger<TestExchangeRateProviderFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _config = config.Value;
        _logger = logger;
    }

    public IExchangeRateProvider GetActiveProvider()
    {
        var activeProviderName = _config.ActiveProvider;
        _logger.LogInformation("Getting active provider: {ProviderName}", activeProviderName);

        // For integration tests, always return our test provider for any configured provider name
        return _serviceProvider.GetRequiredService<TestExchangeRateProvider>();
    }

    public IEnumerable<IExchangeRateProvider> GetAllProviders()
    {
        // Return only our test provider
        return new[] { _serviceProvider.GetRequiredService<TestExchangeRateProvider>() };
    }
}
