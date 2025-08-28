using CurrencyConversionApi.Configuration;
using FluentAssertions;
using Xunit;

namespace CurrencyConversionApi.Tests.Configuration;

public class SmartCacheConfigTests
{
    [Fact]
    public void GetCacheKey_Should_Generate_Correct_Key_For_Latest_Rates()
    {
        // Arrange
        var config = new SmartCacheConfig();
        const string baseCurrency = "USD";

        // Act
        var result = config.GetCacheKey(baseCurrency);

        // Assert
        result.Should().Be("latest_rates_USD");
    }

    [Fact]
    public void GetCacheKey_Should_Throw_For_Historical_Rates()
    {
        // Arrange
        var config = new SmartCacheConfig();
        const string baseCurrency = "USD";

        // Act & Assert
        var act = () => config.GetCacheKey(baseCurrency, isHistorical: true);
        act.Should().Throw<NotSupportedException>()
            .WithMessage("Historical rate caching is disabled due to date range complexity and memory concerns");
    }

    [Fact]
    public void IsBusinessHours_Should_Return_True_During_Business_Hours()
    {
        // Arrange
        var config = new SmartCacheConfig();
        var businessTime = new DateTime(2023, 12, 1, 10, 0, 0); // 10:00 AM

        // Act
        var result = config.IsBusinessHours(businessTime);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsBusinessHours_Should_Return_False_During_Off_Hours()
    {
        // Arrange
        var config = new SmartCacheConfig();
        var offHoursTime = new DateTime(2023, 12, 1, 22, 0, 0); // 10:00 PM

        // Act
        var result = config.IsBusinessHours(offHoursTime);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsBusinessHours_Should_Return_False_At_Business_Hours_End()
    {
        // Arrange
        var config = new SmartCacheConfig();
        var endTime = new DateTime(2023, 12, 1, 20, 0, 0); // 8:00 PM (end of business hours)

        // Act
        var result = config.IsBusinessHours(endTime);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(7, false)]  // Before business hours
    [InlineData(8, true)]   // Start of business hours
    [InlineData(12, true)]  // Midday
    [InlineData(19, true)]  // End of business hours - 1
    [InlineData(20, false)] // End of business hours
    [InlineData(23, false)] // Late night
    public void IsBusinessHours_Should_Handle_Various_Hours(int hour, bool expected)
    {
        // Arrange
        var config = new SmartCacheConfig();
        var testTime = new DateTime(2023, 12, 1, hour, 0, 0);

        // Act
        var result = config.IsBusinessHours(testTime);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void GetOptimalTTL_Should_Return_Business_Hours_TTL_During_Business_Hours()
    {
        // Arrange
        var config = new SmartCacheConfig
        {
            UtcNowProvider = () => new DateTime(2023, 12, 1, 9, 0, 0, DateTimeKind.Utc) // Business hours in CET
        };

        // Act
        var result = config.GetOptimalTTL();

        // Assert
        result.Should().BeLessOrEqualTo(config.BusinessHoursTTL);
    }

    [Fact]
    public void GetOptimalTTL_Should_Return_Off_Hours_TTL_During_Off_Hours()
    {
        // Arrange
        var config = new SmartCacheConfig
        {
            UtcNowProvider = () => new DateTime(2023, 12, 1, 21, 0, 0, DateTimeKind.Utc) // Off hours in CET
        };

        // Act
        var result = config.GetOptimalTTL();

        // Assert
        result.Should().BeLessOrEqualTo(config.OffHoursTTL);
    }

    [Fact]
    public void GetOptimalTTL_Should_Throw_For_Historical_Date()
    {
        // Arrange
        var config = new SmartCacheConfig
        {
            UtcNowProvider = () => new DateTime(2023, 12, 1, 12, 0, 0, DateTimeKind.Utc)
        };
        var historicalDate = new DateTime(2023, 11, 30);

        // Act & Assert
        var act = () => config.GetOptimalTTL(historicalDate);
        act.Should().Throw<NotSupportedException>()
            .WithMessage("Historical rate caching is disabled - serve directly from API");
    }

    [Fact]
    public void GetOptimalTTL_Should_Allow_Today_Date()
    {
        // Arrange
        var testDate = new DateTime(2023, 12, 1, 12, 0, 0, DateTimeKind.Utc);
        var config = new SmartCacheConfig
        {
            UtcNowProvider = () => testDate
        };

        // Act
        var result = config.GetOptimalTTL(testDate.Date);

        // Assert
        result.Should().BePositive();
    }

    [Fact]
    public void Configuration_Properties_Should_Have_Default_Values()
    {
        // Arrange & Act
        var config = new SmartCacheConfig();

        // Assert
        config.FrankfurterUpdateHour.Should().Be(16);
        config.UpdateBuffer.Should().Be(TimeSpan.FromMinutes(5));
        config.BusinessHoursTTL.Should().Be(TimeSpan.FromMinutes(15));
        config.OffHoursTTL.Should().Be(TimeSpan.FromHours(2));
        config.BusinessHoursStart.Should().Be(8);
        config.BusinessHoursEnd.Should().Be(20);
        config.CETTimeZone.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Configuration_Properties_Should_Be_Settable()
    {
        // Arrange
        var config = new SmartCacheConfig();
        var customUtcProvider = () => DateTime.UtcNow;

        // Act
        config.FrankfurterUpdateHour = 18;
        config.UpdateBuffer = TimeSpan.FromMinutes(10);
        config.BusinessHoursTTL = TimeSpan.FromMinutes(30);
        config.OffHoursTTL = TimeSpan.FromHours(4);
        config.BusinessHoursStart = 9;
        config.BusinessHoursEnd = 18;
        config.CETTimeZone = "Europe/Berlin";
        config.UtcNowProvider = customUtcProvider;

        // Assert
        config.FrankfurterUpdateHour.Should().Be(18);
        config.UpdateBuffer.Should().Be(TimeSpan.FromMinutes(10));
        config.BusinessHoursTTL.Should().Be(TimeSpan.FromMinutes(30));
        config.OffHoursTTL.Should().Be(TimeSpan.FromHours(4));
        config.BusinessHoursStart.Should().Be(9);
        config.BusinessHoursEnd.Should().Be(18);
        config.CETTimeZone.Should().Be("Europe/Berlin");
        config.UtcNowProvider.Should().NotBeNull();
        config.UtcNowProvider().Should().BeAfter(DateTime.MinValue);
    }
}
