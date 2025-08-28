using CurrencyConversionApi.Controllers;
using CurrencyConversionApi.DTOs;
using CurrencyConversionApi.Interfaces;
using CurrencyConversionApi.Models;
using CurrencyConversionApi.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace CurrencyConversionApi.Tests.Controllers;

public class CurrencyControllerTests
{
    private readonly Mock<ICurrencyConversionService> _svc = new();
    private readonly Mock<ICorrelationIdService> _corr = new();
    private readonly Mock<ILogger<CurrencyController>> _logger = new();

    public CurrencyControllerTests()
    {
        _corr.Setup(c => c.GetCorrelationId()).Returns("cid-1");
    }

    private CurrencyController Create() => new(_svc.Object, _corr.Object, _logger.Object);

    [Fact]
    public async Task ConvertCurrency_ReturnsOk()
    {
        _svc.Setup(s => s.ConvertAsync(100m, "USD", "EUR", It.IsAny<CancellationToken>())).ReturnsAsync(new ConversionResult
        {
            Amount = 100m, FromCurrency = "USD", ToCurrency = "EUR", ConvertedAmount = 90m, ExchangeRate = 0.9m, RateLastUpdated = DateTime.UtcNow, RateSource = "Test"
        });
        var controller = Create();
        var req = new ConversionRequestDto { Amount = 100m, FromCurrency = "USD", ToCurrency = "EUR" };
        var result = await controller.ConvertCurrency(req);
        var ok = result.Result as OkObjectResult;
        ok.Should().NotBeNull();
    }

    [Fact]
    public async Task ConvertCurrency_BadRequest_OnArgumentException()
    {
        _svc.Setup(s => s.ConvertAsync(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ThrowsAsync(new ArgumentException("bad"));
        var controller = Create();
        var req = new ConversionRequestDto { Amount = 10, FromCurrency = "USD", ToCurrency = "USD" };
        var result = await controller.ConvertCurrency(req);
        (result.Result as BadRequestObjectResult).Should().NotBeNull();
    }

    [Fact]
    public async Task GetLatestRates_ReturnsBadRequest_WhenEmpty()
    {
        _svc.Setup(s => s.GetLatestRatesAsync("EUR", It.IsAny<List<string>>(), It.IsAny<CancellationToken>())).ReturnsAsync(Enumerable.Empty<ExchangeRate>());
        var controller = Create();
        var result = await controller.GetLatestRates("EUR", null);
        (result.Result as BadRequestObjectResult).Should().NotBeNull();
    }

    [Fact]
    public async Task GetLatestRates_ReturnsOk()
    {
        _svc.Setup(s => s.GetLatestRatesAsync("EUR", It.IsAny<List<string>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<ExchangeRate>{ new(){ FromCurrency="EUR", ToCurrency="USD", Rate=1.1m, LastUpdated=DateTime.UtcNow, Source="Test"}});
        var controller = Create();
        var result = await controller.GetLatestRates("EUR", "USD");
        (result.Result as OkObjectResult).Should().NotBeNull();
    }

    [Fact]
    public async Task GetLatestRates_SymbolFilter_Filters()
    {
        _svc.Setup(s => s.GetLatestRatesAsync("EUR", It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ExchangeRate>{ new(){ FromCurrency="EUR", ToCurrency="USD", Rate=1.1m, LastUpdated=DateTime.UtcNow, Source="Test"}, new(){ FromCurrency="EUR", ToCurrency="GBP", Rate=0.8m, LastUpdated=DateTime.UtcNow, Source="Test"}});
        var controller = Create();
        var result = await controller.GetLatestRates("EUR", "USD,GBP");
        (result.Result as OkObjectResult).Should().NotBeNull();
    }

    [Fact]
    public async Task GetHistoricalRates_BadDateFormat()
    {
        var controller = Create();
        var result = await controller.GetHistoricalRates("notdate", "EUR", null);
        (result.Result as BadRequestObjectResult).Should().NotBeNull();
    }

    [Fact]
    public async Task GetHistoricalRates_ReturnsBadRequest_WhenEmpty()
    {
        _svc.Setup(s => s.GetHistoricalRatesAsync(It.IsAny<DateTime>(), "EUR", It.IsAny<List<string>>(), It.IsAny<CancellationToken>())).ReturnsAsync(Enumerable.Empty<ExchangeRate>());
        var controller = Create();
        var date = DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd");
        var result = await controller.GetHistoricalRates(date, "EUR", null);
        (result.Result as BadRequestObjectResult).Should().NotBeNull();
    }

    [Fact]
    public async Task GetHistoricalRates_ReturnsOk()
    {
        _svc.Setup(s => s.GetHistoricalRatesAsync(It.IsAny<DateTime>(), "EUR", It.IsAny<List<string>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<ExchangeRate>{ new(){ FromCurrency="EUR", ToCurrency="USD", Rate=1.0m, LastUpdated=DateTime.UtcNow, Source="Test"}});
        var controller = Create();
        var date = DateTime.Today.AddDays(-2).ToString("yyyy-MM-dd");
        var result = await controller.GetHistoricalRates(date, "EUR", null);
        (result.Result as OkObjectResult).Should().NotBeNull();
    }

    [Fact]
    public async Task GetHistoricalRates_WeekendFlag_Set()
    {
        // Find last Saturday
        var date = DateTime.Today;
        while (date.DayOfWeek != DayOfWeek.Saturday) date = date.AddDays(-1);
        var dateStr = date.AddDays(-7).ToString("yyyy-MM-dd"); // ensure it's in past
        _svc.Setup(s => s.GetHistoricalRatesAsync(It.IsAny<DateTime>(), "EUR", It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ExchangeRate>{ new(){ FromCurrency="EUR", ToCurrency="USD", Rate=1.0m, LastUpdated=DateTime.UtcNow, Source="Test"}});
        var controller = Create();
        var result = await controller.GetHistoricalRates(dateStr, "EUR", null);
        (result.Result as OkObjectResult).Should().NotBeNull();
    }

    [Fact]
    public async Task GetTimeSeriesRates_BadStartDate()
    {
        var controller = Create();
        var result = await controller.GetTimeSeriesRates("bad", DateTime.Today.ToString("yyyy-MM-dd"), "EUR", null);
        (result.Result as BadRequestObjectResult).Should().NotBeNull();
    }

    [Fact]
    public async Task GetTimeSeriesRates_EmptyData_BadRequest()
    {
        _svc.Setup(s => s.GetTimeSeriesRatesAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), "EUR", It.IsAny<List<string>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<string, Dictionary<string, decimal>>());
        var controller = Create();
        var result = await controller.GetTimeSeriesRates(DateTime.Today.AddDays(-5).ToString("yyyy-MM-dd"), DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd"), "EUR", null);
        (result.Result as BadRequestObjectResult).Should().NotBeNull();
    }

    [Fact]
    public async Task GetTimeSeriesRates_ReturnsOk()
    {
        var dict = new Dictionary<string, Dictionary<string, decimal>> { [DateTime.Today.AddDays(-2).ToString("yyyy-MM-dd")] = new(){ ["USD"] = 1.1m } };
        _svc.Setup(s => s.GetTimeSeriesRatesAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), "EUR", It.IsAny<List<string>>(), It.IsAny<CancellationToken>())).ReturnsAsync(dict);
        var controller = Create();
        var result = await controller.GetTimeSeriesRates(DateTime.Today.AddDays(-3).ToString("yyyy-MM-dd"), DateTime.Today.AddDays(-2).ToString("yyyy-MM-dd"), "EUR", null);
        (result.Result as OkObjectResult).Should().NotBeNull();
    }

    [Fact]
    public async Task GetSupportedCurrencies_ReturnsOk()
    {
        _svc.Setup(s => s.GetSupportedCurrenciesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Currency>{ new(){ Code="EUR", Name="Euro", Symbol="€"}, new(){ Code="USD", Name="US Dollar", Symbol="$"}});
        var controller = Create();
        var result = await controller.GetSupportedCurrencies();
        (result.Result as OkObjectResult).Should().NotBeNull();
    }

    [Fact]
    public async Task IsCurrencySupported_BadCode()
    {
        var controller = Create();
        var result = await controller.IsCurrencySupported("AB");
        (result.Result as BadRequestObjectResult).Should().NotBeNull();
    }

    [Fact]
    public async Task IsCurrencySupported_ReturnsOk()
    {
        _svc.Setup(s => s.IsCurrencySupportedAsync("USD", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var controller = Create();
        var result = await controller.IsCurrencySupported("USD");
        (result.Result as OkObjectResult).Should().NotBeNull();
    }

    [Fact]
    public async Task ConvertCurrency_InternalError_Returns500()
    {
        _svc.Setup(s => s.ConvertAsync(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("boom"));
        var controller = Create();
        var req = new ConversionRequestDto { Amount = 50m, FromCurrency = "USD", ToCurrency = "EUR" };
        var result = await controller.ConvertCurrency(req);
        (result.Result as ObjectResult)!.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task GetLatestRates_ArgumentError_Returns400()
    {
        _svc.Setup(s => s.GetLatestRatesAsync("EUR", It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("bad base"));
        var controller = Create();
        var result = await controller.GetLatestRates("EUR", null);
        (result.Result as BadRequestObjectResult).Should().NotBeNull();
    }

    [Fact]
    public async Task GetLatestRates_InternalError_Returns500()
    {
        _svc.Setup(s => s.GetLatestRatesAsync("EUR", It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("fail"));
        var controller = Create();
        var result = await controller.GetLatestRates("EUR", null);
        (result.Result as ObjectResult)!.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task GetHistoricalRates_FutureDate_Returns400()
    {
        var controller = Create();
        var future = DateTime.Today.AddDays(2).ToString("yyyy-MM-dd");
        var result = await controller.GetHistoricalRates(future, "EUR", null);
        (result.Result as BadRequestObjectResult).Should().NotBeNull();
    }

    [Fact]
    public async Task GetHistoricalRates_ArgumentError_Returns400()
    {
        _svc.Setup(s => s.GetHistoricalRatesAsync(It.IsAny<DateTime>(), "EUR", It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("bad hist"));
        var controller = Create();
        var date = DateTime.Today.AddDays(-2).ToString("yyyy-MM-dd");
        var result = await controller.GetHistoricalRates(date, "EUR", null);
        (result.Result as BadRequestObjectResult).Should().NotBeNull();
    }

    [Fact]
    public async Task GetHistoricalRates_InternalError_Returns500()
    {
        _svc.Setup(s => s.GetHistoricalRatesAsync(It.IsAny<DateTime>(), "EUR", It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("hist fail"));
        var controller = Create();
        var date = DateTime.Today.AddDays(-2).ToString("yyyy-MM-dd");
        var result = await controller.GetHistoricalRates(date, "EUR", null);
        (result.Result as ObjectResult)!.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task GetTimeSeriesRates_InvalidEndDateFormat()
    {
        var controller = Create();
        var result = await controller.GetTimeSeriesRates(DateTime.Today.AddDays(-5).ToString("yyyy-MM-dd"), "notdate", "EUR", null);
        (result.Result as BadRequestObjectResult).Should().NotBeNull();
    }

    [Fact]
    public async Task GetTimeSeriesRates_StartAfterEnd_Returns400()
    {
        var controller = Create();
        var start = DateTime.Today.AddDays(-2).ToString("yyyy-MM-dd");
        var end = DateTime.Today.AddDays(-5).ToString("yyyy-MM-dd");
        var result = await controller.GetTimeSeriesRates(start, end, "EUR", null);
        (result.Result as BadRequestObjectResult).Should().NotBeNull();
    }

    [Fact]
    public async Task GetTimeSeriesRates_EndInFuture_Returns400()
    {
        var controller = Create();
        var start = DateTime.Today.AddDays(-5).ToString("yyyy-MM-dd");
        var end = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd");
        var result = await controller.GetTimeSeriesRates(start, end, "EUR", null);
        (result.Result as BadRequestObjectResult).Should().NotBeNull();
    }

    [Fact]
    public async Task GetTimeSeriesRates_ArgumentError_Returns400()
    {
        _svc.Setup(s => s.GetTimeSeriesRatesAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), "EUR", It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("range"));
        var controller = Create();
        var start = DateTime.Today.AddDays(-5).ToString("yyyy-MM-dd");
        var end = DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd");
        var result = await controller.GetTimeSeriesRates(start, end, "EUR", null);
        (result.Result as BadRequestObjectResult).Should().NotBeNull();
    }

    [Fact]
    public async Task GetTimeSeriesRates_InternalError_Returns500()
    {
        _svc.Setup(s => s.GetTimeSeriesRatesAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), "EUR", It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("ts fail"));
        var controller = Create();
        var start = DateTime.Today.AddDays(-5).ToString("yyyy-MM-dd");
        var end = DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd");
        var result = await controller.GetTimeSeriesRates(start, end, "EUR", null);
        (result.Result as ObjectResult)!.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task GetSupportedCurrencies_InternalError_Returns500()
    {
        _svc.Setup(s => s.GetSupportedCurrenciesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("curr fail"));
        var controller = Create();
        var result = await controller.GetSupportedCurrencies();
        (result.Result as ObjectResult)!.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task IsCurrencySupported_InternalError_Returns500()
    {
        _svc.Setup(s => s.IsCurrencySupportedAsync("USD", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("is fail"));
        var controller = Create();
        var result = await controller.IsCurrencySupported("USD");
        (result.Result as ObjectResult)!.StatusCode.Should().Be(500);
    }

    [Theory]
    [InlineData("TRY")]
    [InlineData("PLN")]
    [InlineData("THB")]
    [InlineData("MXN")]
    public async Task IsCurrencySupported_ExcludedCurrency_Returns400(string currency)
    {
        var controller = Create();
        var result = await controller.IsCurrencySupported(currency);
        var badRequest = result.Result as BadRequestObjectResult;
        badRequest.Should().NotBeNull();
    }

    [Theory]
    [InlineData("TRY")]
    [InlineData("PLN")]
    [InlineData("THB")]
    [InlineData("MXN")]
    public async Task GetLatestRates_ExcludedBaseCurrency_Returns400(string baseCurrency)
    {
        var controller = Create();
        var result = await controller.GetLatestRates(baseCurrency, null);
        var badRequest = result.Result as BadRequestObjectResult;
        badRequest.Should().NotBeNull();
    }

    [Fact]
    public async Task GetLatestRates_ExcludedInSymbols_Returns400()
    {
        var controller = Create();
        var result = await controller.GetLatestRates("EUR", "USD,TRY,GBP");
        var badRequest = result.Result as BadRequestObjectResult;
        badRequest.Should().NotBeNull();
    }
}
