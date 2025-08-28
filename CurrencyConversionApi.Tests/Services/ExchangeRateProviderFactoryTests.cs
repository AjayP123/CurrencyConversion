using CurrencyConversionApi.Configuration;
using CurrencyConversionApi.Services;
using CurrencyConversionApi.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CurrencyConversionApi.Tests.Services;

public class ExchangeRateProviderFactoryTests
{
    private readonly Mock<IOptions<ExchangeRateConfig>> _mockOptions;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<ILogger<ExchangeRateProviderFactory>> _mockLogger;
    private readonly ExchangeRateProviderFactory _factory;

    public ExchangeRateProviderFactoryTests()
    {
        _mockOptions = new Mock<IOptions<ExchangeRateConfig>>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockLogger = new Mock<ILogger<ExchangeRateProviderFactory>>();
        _factory = new ExchangeRateProviderFactory(_mockOptions.Object, _mockServiceProvider.Object, _mockLogger.Object);
    }

    [Fact]
    public void Constructor_Should_Initialize_Properties()
    {
        // Arrange
        var config = new ExchangeRateConfig { ActiveProvider = "Frankfurter" };
        _mockOptions.Setup(x => x.Value).Returns(config);

        // Act & Assert - Constructor should complete without throwing
        var factory = new ExchangeRateProviderFactory(
            _mockOptions.Object, 
            _mockServiceProvider.Object, 
            _mockLogger.Object);
        
        factory.Should().NotBeNull();
    }
}

