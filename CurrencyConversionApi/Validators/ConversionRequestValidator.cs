using FluentValidation;
using CurrencyConversionApi.DTOs;

namespace CurrencyConversionApi.Validators;

/// <summary>
/// Validator for conversion requests - excludes TRY, PLN, THB, MXN as per requirements
/// </summary>
public class ConversionRequestValidator : AbstractValidator<ConversionRequestDto>
{
    // Excluded currencies as per requirements
    private static readonly HashSet<string> ExcludedCurrencies = new() { "TRY", "PLN", "THB", "MXN" };
    
    // Common supported currencies (excluding the forbidden ones)
    private static readonly HashSet<string> ValidCurrencies = new()
    {
        "EUR", "USD", "GBP", "JPY", "AUD", "CAD", "CHF", "CNY", "SEK", "NOK",
        "NZD", "KRW", "SGD", "HKD", "INR", "BRL", "ZAR", "CZK", "HUF", "BGN",
        "RON", "HRK", "RUB", "ISK", "PHP", "IDR", "MYR"
    };

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
            .Must(BeValidCurrency)
            .WithMessage("Invalid or unsupported source currency code. TRY, PLN, THB, and MXN are not supported.");

        RuleFor(x => x.ToCurrency)
            .NotEmpty()
            .WithMessage("Target currency is required")
            .Length(3)
            .WithMessage("Currency code must be exactly 3 characters")
            .Must(BeValidCurrency)
            .WithMessage("Invalid or unsupported target currency code. TRY, PLN, THB, and MXN are not supported.");

        RuleFor(x => x)
            .Must(x => x.FromCurrency != x.ToCurrency)
            .WithMessage("Source and target currencies must be different");
    }

    private static bool BeValidCurrency(string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
            return false;

        var upperCurrency = currency.ToUpper();
        
        // Check if it's excluded
        if (ExcludedCurrencies.Contains(upperCurrency))
            return false;

        // For now, accept any 3-letter currency code that's not excluded
        // In a real implementation, you'd check against Frankfurter API supported currencies
        return upperCurrency.Length == 3 && System.Text.RegularExpressions.Regex.IsMatch(upperCurrency, @"^[A-Z]{3}$");
    }
}
