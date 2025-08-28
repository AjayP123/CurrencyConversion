using Microsoft.Extensions.Caching.Memory;
using CurrencyConversionApi.Configuration;
using Microsoft.Extensions.Options;
using CurrencyConversionApi.Models;
using CurrencyConversionApi.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CurrencyConversionApi.Tests.Services;

public class OptimizedCacheServiceTests
{
    private readonly IMemoryCache _memoryCache = new MemoryCache(new MemoryCacheOptions());
    private readonly SmartCacheConfig _smart = new();
    private readonly Mock<ILogger<OptimizedCacheService>> _logger = new();
    private readonly OptimizedCacheService _sut;

    public OptimizedCacheServiceTests()
    {
        _sut = new OptimizedCacheService(_memoryCache, Options.Create(_smart), _logger.Object);
    }

    [Fact]
    public async Task GetLatestRatesAsync_ReturnsNull_WhenEmpty()
    {
        var result = await _sut.GetLatestRatesAsync("EUR");
        result.Should().BeNull();
    }

    [Fact]
    public async Task SetLatestRatesAsync_CachesRates()
    {
        var rates = new List<ExchangeRate>
        {
            new() { FromCurrency = "EUR", ToCurrency = "USD", Rate = 1.1m, Source = "Test", LastUpdated = DateTime.UtcNow }
        };
        await _sut.SetLatestRatesAsync("EUR", rates);
        var cached = await _sut.GetLatestRatesAsync("EUR");
        cached.Should().NotBeNull();
        cached!.First().Rate.Should().Be(1.1m);
    }

    [Fact]
    public async Task GetExchangeRateAsync_ComputesCrossRate()
    {
        var rates = new List<ExchangeRate>
        {
            new() { FromCurrency = "EUR", ToCurrency = "USD", Rate = 1.2m, Source = "Test", LastUpdated = DateTime.UtcNow },
            new() { FromCurrency = "EUR", ToCurrency = "GBP", Rate = 0.8m, Source = "Test", LastUpdated = DateTime.UtcNow }
        };
        await _sut.SetLatestRatesAsync("EUR", rates);
        var rate = await _sut.GetExchangeRateAsync("USD", "GBP");
        rate.Should().NotBeNull();
        // USD->GBP = GBP/EUR divided by USD/EUR => 0.8 / 1.2 = 0.6666...
        rate!.Rate.Should().BeApproximately(0.6666m, 0.0005m);
    }

    [Fact]
    public async Task GetExchangeRateAsync_SameCurrency_ReturnsOne()
    {
        var result = await _sut.GetExchangeRateAsync("USD", "USD");
        result.Should().NotBeNull();
        result!.Rate.Should().Be(1.0m);
    }
}
