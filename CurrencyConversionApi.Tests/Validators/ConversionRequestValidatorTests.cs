using CurrencyConversionApi.DTOs;
using CurrencyConversionApi.Validators;
using FluentAssertions;
using Xunit;

namespace CurrencyConversionApi.Tests.Validators;

public class ConversionRequestValidatorTests
{
    private readonly ConversionRequestValidator _validator = new();

    [Fact]
    public void Valid_Request_Passes()
    {
        var dto = new ConversionRequestDto { Amount = 123.45m, FromCurrency = "USD", ToCurrency = "EUR" };
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Invalid_Amount_Fails(decimal amount)
    {
        var dto = new ConversionRequestDto { Amount = amount, FromCurrency = "USD", ToCurrency = "EUR" };
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ConversionRequestDto.Amount));
    }

    [Fact]
    public void Excluded_Currency_Fails()
    {
        var dto = new ConversionRequestDto { Amount = 10, FromCurrency = "TRY", ToCurrency = "EUR" };
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Same_Currency_Fails()
    {
        var dto = new ConversionRequestDto { Amount = 10, FromCurrency = "USD", ToCurrency = "USD" };
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Invalid_Code_Length_Fails()
    {
        var dto = new ConversionRequestDto { Amount = 10, FromCurrency = "US", ToCurrency = "EUR" };
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
    }
}
