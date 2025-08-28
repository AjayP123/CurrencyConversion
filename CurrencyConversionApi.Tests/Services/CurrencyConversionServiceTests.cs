using CurrencyConversionApi.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using CurrencyConversionApi.Configuration;
using CurrencyConversionApi.Services;

namespace CurrencyConversionApi.Tests.Services;

public class CurrencyConversionServiceTests
{
	private readonly Mock<IExchangeRateProvider> _provider1 = new();
	private readonly Mock<IExchangeRateProvider> _provider2 = new();
	private readonly Mock<ICacheService> _cache = new();
	private readonly Mock<ILogger<CurrencyConversionService>> _logger = new();
	private readonly Mock<IExchangeRateProviderFactory> _providerFactory = new();
	private readonly CurrencyConversionService _sut;

	public CurrencyConversionServiceTests()
	{
		_provider1.Setup(p => p.ProviderName).Returns("AProvider");
		_provider2.Setup(p => p.ProviderName).Returns("BProvider");
		
		// Setup factory to return provider1 as the active provider by default
		_providerFactory.Setup(f => f.GetActiveProvider()).Returns(_provider1.Object);
		_providerFactory.Setup(f => f.GetAllProviders()).Returns(new[] { _provider1.Object, _provider2.Object });
		
		_sut = new CurrencyConversionService(_providerFactory.Object, _cache.Object, _logger.Object);
	}

	private static Mock<IExchangeRateProviderFactory> CreateFactoryMock(params IExchangeRateProvider[] providers)
	{
		var factory = new Mock<IExchangeRateProviderFactory>();
		if (providers.Length > 0)
		{
			factory.Setup(f => f.GetActiveProvider()).Returns(providers[0]);
		}
		factory.Setup(f => f.GetAllProviders()).Returns(providers);
		return factory;
	}

	[Fact]
	public async Task ConvertAsync_ReturnsSameAmount_ForSameCurrency()
	{
		var result = await _sut.ConvertAsync(100m, "USD", "USD");
		result.ConvertedAmount.Should().Be(100m);
		result.ExchangeRate.Should().Be(1m);
	}

	[Fact]
	public async Task ConvertAsync_Throws_ForZeroAmount()
	{
		await FluentActions.Invoking(() => _sut.ConvertAsync(0m, "USD", "EUR"))
			.Should().ThrowAsync<ArgumentException>();
	}

	[Fact]
	public async Task ConvertAsync_UsesProviderRate_WhenAvailable()
	{
		_cache.Setup(c => c.GetExchangeRateAsync("USD", "EUR"))
			.ReturnsAsync((ExchangeRate?)null);
		_provider1.Setup(p => p.GetRateAsync("USD", "EUR", It.IsAny<CancellationToken>()))
			.ReturnsAsync(new ExchangeRate { FromCurrency = "USD", ToCurrency = "EUR", Rate = 0.9m, LastUpdated = DateTime.UtcNow, Source = "AProvider" });

		var result = await _sut.ConvertAsync(100m, "USD", "EUR");
		result.ConvertedAmount.Should().Be(90m);
		result.RateSource.Should().Be("AProvider");
	}

	[Fact]
	public async Task GetLatestRatesAsync_ReturnsCached_WhenPresent()
	{
		var cached = new List<ExchangeRate> { new() { FromCurrency = "EUR", ToCurrency = "USD", Rate = 1.1m, Source = "Cache" } };
		_cache.Setup(c => c.GetLatestRatesAsync("EUR")).ReturnsAsync(cached);

		var rates = await _sut.GetLatestRatesAsync("EUR");
		rates.Should().HaveCount(1);
		rates.First().Rate.Should().Be(1.1m);
	}

	[Fact]
	public async Task GetLatestRatesAsync_FallsBackToProviders_WhenCacheEmpty()
	{
		_cache.Setup(c => c.GetLatestRatesAsync("EUR")).ReturnsAsync((IEnumerable<ExchangeRate>?)null);
		_provider1.Setup(p => p.GetLatestRatesAsync("EUR", null, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new List<ExchangeRate> { new() { FromCurrency = "EUR", ToCurrency = "USD", Rate = 1.05m, Source = "AProvider" } });

		var rates = await _sut.GetLatestRatesAsync("EUR");
		rates.Should().HaveCount(1);
		rates.First().Rate.Should().Be(1.05m);
		_cache.Verify(c => c.SetLatestRatesAsync("EUR", It.IsAny<IEnumerable<ExchangeRate>>(), null), Times.Once);
	}

	[Fact]
	public async Task GetExchangeRateAsync_ReturnsCachedRate()
	{
		_cache.Setup(c => c.GetExchangeRateAsync("USD", "EUR"))
			.ReturnsAsync(new ExchangeRate { FromCurrency = "USD", ToCurrency = "EUR", Rate = 0.8m, Source = "Cache" });
		var rate = await _sut.GetExchangeRateAsync("USD", "EUR");
		rate!.Rate.Should().Be(0.8m);
	}

	[Fact]
	public async Task GetExchangeRateAsync_ReturnsNull_WhenProvidersFail()
	{
		var cacheMock = new Mock<ICacheService>();
		cacheMock.Setup(c => c.GetExchangeRateAsync("USD", "JPY")).ReturnsAsync((ExchangeRate?)null);

		var badProvider = new Mock<IExchangeRateProvider>();
		badProvider.Setup(p => p.ProviderName).Returns("ZProvider");
		badProvider.Setup(p => p.GetRateAsync("USD", "JPY", It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("boom"));

		var factoryMock = CreateFactoryMock(badProvider.Object);
		var svc = new CurrencyConversionService(factoryMock.Object, cacheMock.Object, _logger.Object);
		var rate = await svc.GetExchangeRateAsync("USD", "JPY");
		rate.Should().BeNull();
	}

	[Fact]
	public async Task GetHistoricalRatesAsync_FutureDate_Throws()
	{
		var factoryMock = CreateFactoryMock();
		var svc = new CurrencyConversionService(factoryMock.Object, _cache.Object, _logger.Object);
		Func<Task> act = async () => await svc.GetHistoricalRatesAsync(DateTime.Today.AddDays(1));
		await act.Should().ThrowAsync<ArgumentException>();
	}

	[Fact]
	public async Task GetTimeSeriesRatesAsync_InvalidDates_Throw()
	{
		var factoryMock = CreateFactoryMock();
		var svc = new CurrencyConversionService(factoryMock.Object, _cache.Object, _logger.Object);
		Func<Task> act1 = async () => await svc.GetTimeSeriesRatesAsync(DateTime.Today.AddDays(1), DateTime.Today);
		await act1.Should().ThrowAsync<ArgumentException>();
		// start > end
		Func<Task> act2 = async () => await svc.GetTimeSeriesRatesAsync(DateTime.Today, DateTime.Today.AddDays(-1));
		await act2.Should().ThrowAsync<ArgumentException>();
	}

	[Fact]
	public async Task GetExchangeRateAsync_ActiveProviderFails_ReturnsNull()
	{
		_cache.Setup(c => c.GetExchangeRateAsync("USD", "GBP")).ReturnsAsync((ExchangeRate?)null);
		_provider1.Setup(p => p.ProviderName).Returns("AProvider");
		_provider1.Setup(p => p.GetRateAsync("USD", "GBP", It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("fail"));
		
		// Since we only use active provider now, if it fails, we get null
		var rate = await _sut.GetExchangeRateAsync("USD", "GBP");
		rate.Should().BeNull();
	}

	[Fact]
	public async Task HistoricalRates_Success_FromProvider()
	{
		var histProvider = new Mock<IExchangeRateProvider>();
		histProvider.Setup(p => p.ProviderName).Returns("HProvider");
		histProvider.Setup(p => p.GetHistoricalRatesAsync(It.IsAny<DateTime>(), "EUR", It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(new List<ExchangeRate>{ new(){ FromCurrency="EUR", ToCurrency="USD", Rate=1.1m, LastUpdated=DateTime.UtcNow, Source="HProvider"}});
		var factoryMock = CreateFactoryMock(histProvider.Object);
		var svc = new CurrencyConversionService(factoryMock.Object, _cache.Object, _logger.Object);
		var data = await svc.GetHistoricalRatesAsync(DateTime.Today.AddDays(-2), "EUR", null);
		data.Should().HaveCount(1);
	}

	[Fact]
	public async Task TimeSeriesRates_Success_FromProvider()
	{
		var tsProvider = new Mock<IExchangeRateProvider>();
		tsProvider.Setup(p => p.ProviderName).Returns("TProvider");
		tsProvider.Setup(p => p.GetTimeSeriesRatesAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), "EUR", It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(new Dictionary<string, Dictionary<string, decimal>> { [DateTime.Today.AddDays(-2).ToString("yyyy-MM-dd")] = new(){ ["USD"] = 1.1m } });
		var factoryMock = CreateFactoryMock(tsProvider.Object);
		var svc = new CurrencyConversionService(factoryMock.Object, _cache.Object, _logger.Object);
		var data = await svc.GetTimeSeriesRatesAsync(DateTime.Today.AddDays(-3), DateTime.Today.AddDays(-2), "EUR", null);
		data.Should().HaveCount(1);
	}

	[Fact]
	public async Task IsCurrencySupportedAsync_TrueFalse()
	{
		var factoryMock = CreateFactoryMock();
		var svc = new CurrencyConversionService(factoryMock.Object, _cache.Object, _logger.Object);
		var ok = await svc.IsCurrencySupportedAsync("USD");
		var bad = await svc.IsCurrencySupportedAsync("ZZZ");
		ok.Should().BeTrue();
		bad.Should().BeFalse();
	}

	[Fact]
	public async Task GetLatestRatesAsync_ProviderSymbolsFilter_Applied()
	{
		_cache.Setup(c => c.GetLatestRatesAsync("EUR")).ReturnsAsync((IEnumerable<ExchangeRate>?)null);
		_provider1.Setup(p => p.ProviderName).Returns("AProvider");
		_provider1.Setup(p => p.GetLatestRatesAsync("EUR", null, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new List<ExchangeRate>{ new(){ FromCurrency="EUR", ToCurrency="USD", Rate=1.1m, Source="AProvider", LastUpdated=DateTime.UtcNow}, new(){ FromCurrency="EUR", ToCurrency="GBP", Rate=0.8m, Source="AProvider", LastUpdated=DateTime.UtcNow}});
		var factoryMock = CreateFactoryMock(_provider1.Object);
		var svc = new CurrencyConversionService(factoryMock.Object, _cache.Object, _logger.Object);
		var rates = await svc.GetLatestRatesAsync("EUR", new List<string>{ "USD" });
		rates.Should().HaveCount(1);
	}

	[Fact]
	public async Task GetExchangeRateAsync_SameCurrency_ReturnsDirect()
	{
		var rate = await _sut.GetExchangeRateAsync("USD", "USD");
		rate.Should().NotBeNull();
		rate!.Rate.Should().Be(1m);
		rate.Source.Should().Be("Direct");
	}

	[Fact]
	public async Task ConvertAsync_RateNotFound_Throws()
	{
		_cache.Setup(c => c.GetExchangeRateAsync("AAA", "BBB")).ReturnsAsync((ExchangeRate?)null);
		_provider1.Setup(p => p.ProviderName).Returns("AProvider");
		_provider2.Setup(p => p.ProviderName).Returns("BProvider");
		_provider1.Setup(p => p.GetRateAsync("AAA", "BBB", It.IsAny<CancellationToken>())).ReturnsAsync((ExchangeRate?)null);
		_provider2.Setup(p => p.GetRateAsync("AAA", "BBB", It.IsAny<CancellationToken>())).ReturnsAsync((ExchangeRate?)null);
		await FluentActions.Invoking(() => _sut.ConvertAsync(10m, "AAA", "BBB"))
			.Should().ThrowAsync<InvalidOperationException>();
	}

	[Fact]
	public async Task ConvertAsync_InvalidCurrency_Throws()
	{
		await FluentActions.Invoking(() => _sut.ConvertAsync(10m, "US", "EUR"))
			.Should().ThrowAsync<ArgumentException>();
	}

	[Fact]
	public async Task GetLatestRatesAsync_AllProvidersFail_ReturnsEmpty()
	{
		_cache.Setup(c => c.GetLatestRatesAsync("EUR")).ReturnsAsync((IEnumerable<ExchangeRate>?)null);
		_provider1.Setup(p => p.ProviderName).Returns("AProvider");
		_provider2.Setup(p => p.ProviderName).Returns("BProvider");
		_provider1.Setup(p => p.GetLatestRatesAsync("EUR", null, It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("boom1"));
		_provider2.Setup(p => p.GetLatestRatesAsync("EUR", null, It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("boom2"));
		var rates = await _sut.GetLatestRatesAsync("EUR");
		rates.Should().BeEmpty();
	}

	[Fact]
	public async Task HistoricalRates_AllProvidersFail_ReturnsEmpty()
	{
		var badProvider = new Mock<IExchangeRateProvider>();
		badProvider.Setup(p => p.ProviderName).Returns("BadHist");
		badProvider.Setup(p => p.GetHistoricalRatesAsync(It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
			.ThrowsAsync(new Exception("hist fail"));
		var factoryMock = CreateFactoryMock(badProvider.Object);
		var svc = new CurrencyConversionService(factoryMock.Object, _cache.Object, _logger.Object);
		var result = await svc.GetHistoricalRatesAsync(DateTime.Today.AddDays(-5), "EUR", null);
		result.Should().BeEmpty();
	}

	[Fact]
	public async Task TimeSeries_AllProvidersFail_ReturnsEmpty()
	{
		var badProvider = new Mock<IExchangeRateProvider>();
		badProvider.Setup(p => p.ProviderName).Returns("BadTS");
		badProvider.Setup(p => p.GetTimeSeriesRatesAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
			.ThrowsAsync(new Exception("ts fail"));
		var factoryMock = CreateFactoryMock(badProvider.Object);
		var svc = new CurrencyConversionService(factoryMock.Object, _cache.Object, _logger.Object);
		var dict = await svc.GetTimeSeriesRatesAsync(DateTime.Today.AddDays(-5), DateTime.Today.AddDays(-3), "EUR", null);
		dict.Should().BeEmpty();
	}

	[Fact]
	public async Task ConvertAsync_RoundsUsingCurrencyDecimalPlaces_JPY()
	{
		_cache.Setup(c => c.GetExchangeRateAsync("USD", "JPY")).ReturnsAsync((ExchangeRate?)null);
		_provider1.Setup(p => p.ProviderName).Returns("AProvider");
		_provider1.Setup(p => p.GetRateAsync("USD", "JPY", It.IsAny<CancellationToken>())).ReturnsAsync(new ExchangeRate { FromCurrency = "USD", ToCurrency = "JPY", Rate = 150.1234m, LastUpdated = DateTime.UtcNow, Source = "AProvider" });
		var amount = 1.234m; // should multiply then round to 0 decimals
		var result = await _sut.ConvertAsync(amount, "USD", "JPY");
		result.ConvertedAmount.Should().Be(Math.Round(amount * 150.1234m, 0));
	}

	[Fact]
	public async Task IsCurrencySupportedAsync_InvalidShortCode_ReturnsFalse()
	{
		var factoryMock = CreateFactoryMock();
		var svc = new CurrencyConversionService(factoryMock.Object, _cache.Object, _logger.Object);
		var ok = await svc.IsCurrencySupportedAsync("AB");
		ok.Should().BeFalse();
	}
}
