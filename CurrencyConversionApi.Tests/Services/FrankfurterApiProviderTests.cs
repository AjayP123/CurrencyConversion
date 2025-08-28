using CurrencyConversionApi.Configuration;
using Microsoft.Extensions.Options;
using CurrencyConversionApi.Models;
using CurrencyConversionApi.Services;
using System.Net;
using System.Net.Http;
using Moq;
using Moq.Protected;
using FluentAssertions;
using Microsoft.Extensions.Logging;

namespace CurrencyConversionApi.Tests.Services;

public class FrankfurterApiProviderHttpTests
{
    private FrankfurterApiProvider CreateProvider(HttpResponseMessage responseMessage)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMessage);

        var httpClient = new HttpClient(handler.Object)
        {
            BaseAddress = new Uri("https://api.frankfurter.app/"),
            Timeout = TimeSpan.FromSeconds(5)
        };

        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("FrankfurterApi")).Returns(httpClient);

    var cfg = Options.Create(new ExchangeRateConfig());
        var logger = Mock.Of<ILogger<FrankfurterApiProvider>>();

        return new FrankfurterApiProvider(factory.Object, cfg, logger);
    }

    [Fact]
    public async Task GetRateAsync_ReturnsRate_OnSuccess()
    {
        var json = System.Text.Json.JsonSerializer.Serialize(new { amount = 1, @base = "USD", date = DateTime.UtcNow.ToString("yyyy-MM-dd"), rates = new { EUR = 0.92m } });
    var provider = CreateProvider(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) });
        var rate = await provider.GetRateAsync("USD", "EUR");
        rate.Should().NotBeNull();
        rate!.Rate.Should().Be(0.92m);
    }

    [Fact]
    public async Task GetRateAsync_ReturnsNull_WhenMissingRate()
    {
        var json = System.Text.Json.JsonSerializer.Serialize(new { amount = 1, @base = "USD", date = DateTime.UtcNow.ToString("yyyy-MM-dd"), rates = new { GBP = 0.80m } });
    var provider = CreateProvider(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) });
        var rate = await provider.GetRateAsync("USD", "EUR");
        rate.Should().BeNull();
    }

    [Fact]
    public async Task GetLatestRatesAsync_FiltersExcluded()
    {
        var json = System.Text.Json.JsonSerializer.Serialize(new { amount = 1, @base = "EUR", date = DateTime.UtcNow.ToString("yyyy-MM-dd"), rates = new { USD = 1.1m, TRY = 35m } });
    var provider = CreateProvider(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) });
        var rates = await provider.GetLatestRatesAsync("EUR");
        rates.Should().HaveCount(1);
        rates.First().ToCurrency.Should().Be("USD");
    }
}

public class FrankfurterApiProviderExtendedTests
{
    private FrankfurterApiProvider CreateProvider(HttpResponseMessage responseMessage)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMessage);
        var httpClient = new HttpClient(handler.Object)
        {
            BaseAddress = new Uri("https://api.frankfurter.app/"),
            Timeout = TimeSpan.FromSeconds(5)
        };
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("FrankfurterApi")).Returns(httpClient);
        var cfg = Options.Create(new ExchangeRateConfig());
        var logger = Mock.Of<ILogger<FrankfurterApiProvider>>();
        return new FrankfurterApiProvider(factory.Object, cfg, logger);
    }

    [Fact]
    public async Task GetHistoricalRatesAsync_ReturnsData()
    {
        var json = System.Text.Json.JsonSerializer.Serialize(new { amount = 1, _base = "EUR", Base = "EUR", date = DateTime.UtcNow.AddDays(-2).ToString("yyyy-MM-dd"), rates = new { USD = 1.1m, TRY = 35m } });
        // FrankfurterLatestResponse expects properties camelCase: base,date,rates
        json = json.Replace("_base", "base");
        var provider = CreateProvider(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) });
        var rates = await provider.GetHistoricalRatesAsync(DateTime.UtcNow.AddDays(-2), "EUR");
        rates.Should().HaveCount(1); // TRY excluded
    }

    [Fact]
    public async Task GetTimeSeriesRatesAsync_ReturnsFiltered()
    {
        var start = DateTime.UtcNow.AddDays(-3).ToString("yyyy-MM-dd");
        var end = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd");
        var payload = new
        {
            _base = "EUR",
            start_date = start,
            end_date = end,
            rates = new Dictionary<string, Dictionary<string, decimal>>
            {
                [start] = new() { { "USD", 1.1m }, { "TRY", 35m } },
                [end] = new() { { "USD", 1.09m } }
            }
        };
        var json = System.Text.Json.JsonSerializer.Serialize(payload).Replace("_base", "base");
        var provider = CreateProvider(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) });
        var data = await provider.GetTimeSeriesRatesAsync(DateTime.UtcNow.AddDays(-3), DateTime.UtcNow.AddDays(-1), "EUR");
        data.Should().HaveCount(2);
        data.Values.First().Keys.Should().NotContain("TRY");
    }

    [Fact]
    public async Task GetRateAsync_Throws_OnHttpError()
    {
        var provider = CreateProvider(new HttpResponseMessage(HttpStatusCode.BadGateway));
        await FluentActions.Invoking(() => provider.GetRateAsync("USD", "EUR")).Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task GetLatestRatesAsync_Throws_OnTimeout()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("timeout"));
        var httpClient = new HttpClient(handler.Object)
        {
            BaseAddress = new Uri("https://api.frankfurter.app/"),
            Timeout = TimeSpan.FromMilliseconds(10)
        };
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("FrankfurterApi")).Returns(httpClient);
        var cfg = Options.Create(new ExchangeRateConfig());
        var logger = Mock.Of<ILogger<FrankfurterApiProvider>>();
        var provider = new FrankfurterApiProvider(factory.Object, cfg, logger);
    await FluentActions.Invoking(() => provider.GetLatestRatesAsync("EUR")).Should().ThrowAsync<TaskCanceledException>();
    }
}

public class FrankfurterApiProviderSymbolFilterTests
{
    private FrankfurterApiProvider CreateProvider(HttpResponseMessage responseMessage)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMessage);
        var httpClient = new HttpClient(handler.Object)
        {
            BaseAddress = new Uri("https://api.frankfurter.app/"),
            Timeout = TimeSpan.FromSeconds(5)
        };
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("FrankfurterApi")).Returns(httpClient);
        var cfg = Options.Create(new ExchangeRateConfig());
        var logger = Mock.Of<ILogger<FrankfurterApiProvider>>();
        return new FrankfurterApiProvider(factory.Object, cfg, logger);
    }

    [Fact]
    public async Task GetLatestRatesAsync_WithSymbols_BuildsAndReturnsFiltered()
    {
        var json = System.Text.Json.JsonSerializer.Serialize(new { amount = 1, _base = "EUR", Base = "EUR", date = DateTime.UtcNow.ToString("yyyy-MM-dd"), rates = new { USD = 1.1m, GBP = 0.8m, TRY = 35m } });
        json = json.Replace("_base", "base");
        var provider = CreateProvider(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) });
        var rates = await provider.GetLatestRatesAsync("EUR", new List<string>{ "USD", "GBP", "TRY" });
        rates.Should().HaveCount(2); // TRY excluded
    }
}
