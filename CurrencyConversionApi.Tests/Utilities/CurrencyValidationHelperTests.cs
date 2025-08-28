using CurrencyConversionApi.Utilities;
using FluentAssertions;
using Xunit;

namespace CurrencyConversionApi.Tests.Utilities;

public class CurrencyValidationHelperTests
{
    [Theory]
    [InlineData("TRY")]
    [InlineData("PLN")]
    [InlineData("THB")]
    [InlineData("MXN")]
    [InlineData("try")]
    [InlineData("pln")]
    [InlineData("thb")]
    [InlineData("mxn")]
    public void IsValidCurrency_ExcludedCurrencies_ReturnsFalse(string currency)
    {
        // Act
        var result = CurrencyValidationHelper.IsValidCurrency(currency);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("USD")]
    [InlineData("EUR")]
    [InlineData("GBP")]
    [InlineData("JPY")]
    [InlineData("usd")]
    [InlineData("eur")]
    public void IsValidCurrency_SupportedCurrencies_ReturnsTrue(string currency)
    {
        // Act
        var result = CurrencyValidationHelper.IsValidCurrency(currency);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    [InlineData("US")]
    [InlineData("USDX")]
    [InlineData("123")]
    [InlineData("US1")]
    public void IsValidCurrency_InvalidFormat_ReturnsFalse(string currency)
    {
        // Act
        var result = CurrencyValidationHelper.IsValidCurrency(currency);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("TRY")]
    [InlineData("PLN")]
    [InlineData("THB")]
    [InlineData("MXN")]
    public void ValidateAndNormalizeCurrency_ExcludedCurrencies_ThrowsArgumentException(string currency)
    {
        // Act
        var act = () => CurrencyValidationHelper.ValidateAndNormalizeCurrency(currency, "testParam");

        // Assert
        act.Should().Throw<ArgumentException>()
           .WithMessage($"Currency {currency.ToUpper()} is not supported*")
           .And.ParamName.Should().Be("testParam");
    }

    [Theory]
    [InlineData("USD", "USD")]
    [InlineData("usd", "USD")]
    [InlineData(" EUR ", "EUR")]
    [InlineData("gbp", "GBP")]
    public void ValidateAndNormalizeCurrency_ValidCurrencies_ReturnsNormalized(string input, string expected)
    {
        // Act
        var result = CurrencyValidationHelper.ValidateAndNormalizeCurrency(input, "testParam");

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void ValidateAndNormalizeCurrency_EmptyOrNull_ThrowsArgumentException(string currency)
    {
        // Act
        var act = () => CurrencyValidationHelper.ValidateAndNormalizeCurrency(currency, "testParam");

        // Assert
        act.Should().Throw<ArgumentException>()
           .WithMessage("Currency code cannot be null or empty*")
           .And.ParamName.Should().Be("testParam");
    }

    [Theory]
    [InlineData("US")]
    [InlineData("USDX")]
    public void ValidateAndNormalizeCurrency_InvalidLength_ThrowsArgumentException(string currency)
    {
        // Act
        var act = () => CurrencyValidationHelper.ValidateAndNormalizeCurrency(currency, "testParam");

        // Assert
        act.Should().Throw<ArgumentException>()
           .WithMessage("Currency code must be exactly 3 characters*")
           .And.ParamName.Should().Be("testParam");
    }

    [Fact]
    public void ValidateAndNormalizeCurrencies_WithExcludedCurrency_ThrowsArgumentException()
    {
        // Arrange
        var currencies = new[] { "USD", "EUR", "TRY" };

        // Act
        var act = () => CurrencyValidationHelper.ValidateAndNormalizeCurrencies(currencies, "testParam");

        // Assert
        act.Should().Throw<ArgumentException>()
           .WithMessage("Currency TRY is not supported*");
    }

    [Fact]
    public void ValidateAndNormalizeCurrencies_ValidCurrencies_ReturnsNormalizedList()
    {
        // Arrange
        var currencies = new[] { "usd", " EUR ", "gbp" };

        // Act
        var result = CurrencyValidationHelper.ValidateAndNormalizeCurrencies(currencies, "testParam");

        // Assert
        result.Should().BeEquivalentTo(new[] { "USD", "EUR", "GBP" });
    }

    [Theory]
    [InlineData("TRY")]
    [InlineData("try")]
    [InlineData(null)]
    public void GetExclusionErrorMessage_ReturnsCorrectMessage(string currency)
    {
        // Act
        var result = CurrencyValidationHelper.GetExclusionErrorMessage(currency);

        // Assert
        result.Should().Contain("is not supported");
        result.Should().Contain("TRY, PLN, THB, MXN");
    }

    [Fact]
    public void ExcludedCurrencies_ContainsExpectedCurrencies()
    {
        // Arrange & Act
        var excluded = CurrencyValidationHelper.ExcludedCurrencies;

        // Assert
        excluded.Should().HaveCount(4);
        excluded.Should().Contain(new[] { "TRY", "PLN", "THB", "MXN" });
    }
}
