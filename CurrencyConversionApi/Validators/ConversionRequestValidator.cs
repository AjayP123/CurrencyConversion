using FluentValidation;
using CurrencyConversionApi.DTOs;
using CurrencyConversionApi.Utilities;

namespace CurrencyConversionApi.Validators;

/// <summary>
/// Validator for conversion requests - excludes TRY, PLN, THB, MXN as per requirements
/// </summary>
public class ConversionRequestValidator : AbstractValidator<ConversionRequestDto>
{
    public ConversionRequestValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than 0")
            .LessThanOrEqualTo(1_000_000)
            .WithMessage("Amount cannot exceed 1,000,000");

        RuleFor(x => x.FromCurrency)
            .NotEmpty()
            .WithMessage("Source currency is required")
            .Length(3)
            .WithMessage("Currency code must be exactly 3 characters")
            .Must(CurrencyValidationHelper.IsValidCurrency)
            .WithMessage($"Invalid or unsupported source currency code. Excluded currencies: {string.Join(", ", CurrencyValidationHelper.ExcludedCurrencies)}");

        RuleFor(x => x.ToCurrency)
            .NotEmpty()
            .WithMessage("Target currency is required")
            .Length(3)
            .WithMessage("Currency code must be exactly 3 characters")
            .Must(CurrencyValidationHelper.IsValidCurrency)
            .WithMessage($"Invalid or unsupported target currency code. Excluded currencies: {string.Join(", ", CurrencyValidationHelper.ExcludedCurrencies)}");

        RuleFor(x => x)
            .Must(x => x.FromCurrency != x.ToCurrency)
            .WithMessage("Source and target currencies must be different");
    }
}
