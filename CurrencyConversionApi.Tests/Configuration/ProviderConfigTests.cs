using CurrencyConversionApi.Configuration;
using FluentAssertions;
using Xunit;

namespace CurrencyConversionApi.Tests.Configuration;

public class ProviderConfigTests
{
    [Fact]
    public void ProviderConfig_Properties_Should_Be_Set_Correctly()
    {
        // Arrange
        const string baseUrl = "https://api.example.com/";
        const string apiKey = "test-api-key";
        const bool enabled = true;
        const int priority = 1;

        // Act
        var config = new ProviderConfig
        {
            BaseUrl = baseUrl,
            ApiKey = apiKey,
            Enabled = enabled,
            Priority = priority
        };

        // Assert
        config.BaseUrl.Should().Be(baseUrl);
        config.ApiKey.Should().Be(apiKey);
        config.Enabled.Should().Be(enabled);
        config.Priority.Should().Be(priority);
    }

    [Fact]
    public void ProviderConfig_Default_Values_Should_Be_Correct()
    {
        // Arrange & Act
        var config = new ProviderConfig();

        // Assert
        config.BaseUrl.Should().Be(string.Empty);
        config.ApiKey.Should().Be(string.Empty);
        config.Enabled.Should().BeTrue();
        config.RateLimitPerMinute.Should().Be(60);
        config.Priority.Should().Be(1);
    }

    [Theory]
    [InlineData("https://api.frankfurter.app/", null, true, 1)]
    [InlineData("https://api.exchangerate-api.com/", "secret-key", false, 2)]
    [InlineData("https://api.currencyapi.com/", "another-key", true, 3)]
    public void ProviderConfig_Should_Handle_Various_Configurations(
        string baseUrl, string? apiKey, bool enabled, int priority)
    {
        // Arrange & Act
        var config = new ProviderConfig
        {
            BaseUrl = baseUrl,
            ApiKey = apiKey,
            Enabled = enabled,
            Priority = priority
        };

        // Assert
        config.BaseUrl.Should().Be(baseUrl);
        config.ApiKey.Should().Be(apiKey);
        config.Enabled.Should().Be(enabled);
        config.Priority.Should().Be(priority);
    }
}
