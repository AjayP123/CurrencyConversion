using System.Text.RegularExpressions;

namespace CurrencyConversionApi.Utilities;

/// <summary>
/// Helper class for currency validation and exclusion logic
/// </summary>
public static class CurrencyValidationHelper
{
    /// <summary>
    /// Currencies excluded from the API as per business requirements
    /// </summary>
    public static readonly HashSet<string> ExcludedCurrencies = new() { "TRY", "PLN", "THB", "MXN" };

    /// <summary>
    /// Validates if a currency code is supported (not excluded) and properly formatted
    /// </summary>
    /// <param name="currencyCode">Currency code to validate</param>
    /// <returns>True if valid and not excluded, false otherwise</returns>
    public static bool IsValidCurrency(string currencyCode)
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
            return false;

        var upperCurrency = currencyCode.ToUpper().Trim();

        // Check if it's excluded
        if (ExcludedCurrencies.Contains(upperCurrency))
            return false;

        // Check format: 3 uppercase letters
        return upperCurrency.Length == 3 && 
               Regex.IsMatch(upperCurrency, @"^[A-Z]{3}$", RegexOptions.Compiled);
    }

    /// <summary>
    /// Validates a currency code and throws ArgumentException if invalid or excluded
    /// </summary>
    /// <param name="currencyCode">Currency code to validate</param>
    /// <param name="paramName">Parameter name for exception</param>
    /// <returns>Normalized (uppercase, trimmed) currency code</returns>
    /// <exception cref="ArgumentException">Thrown if currency is invalid or excluded</exception>
    public static string ValidateAndNormalizeCurrency(string currencyCode, string paramName)
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
            throw new ArgumentException("Currency code cannot be null or empty", paramName);

        var normalizedCurrency = currencyCode.ToUpper().Trim();

        if (normalizedCurrency.Length != 3)
            throw new ArgumentException("Currency code must be exactly 3 characters", paramName);

        if (!Regex.IsMatch(normalizedCurrency, @"^[A-Z]{3}$"))
            throw new ArgumentException("Currency code must contain only letters", paramName);

        if (ExcludedCurrencies.Contains(normalizedCurrency))
            throw new ArgumentException($"Currency {normalizedCurrency} is not supported. Excluded currencies: {string.Join(", ", ExcludedCurrencies)}", paramName);

        return normalizedCurrency;
    }

    /// <summary>
    /// Validates a list of currency codes
    /// </summary>
    /// <param name="currencyCodes">List of currency codes to validate</param>
    /// <param name="paramName">Parameter name for exception</param>
    /// <returns>Normalized list of currency codes</returns>
    /// <exception cref="ArgumentException">Thrown if any currency is invalid or excluded</exception>
    public static List<string> ValidateAndNormalizeCurrencies(IEnumerable<string> currencyCodes, string paramName)
    {
        var result = new List<string>();
        
        foreach (var currency in currencyCodes)
        {
            result.Add(ValidateAndNormalizeCurrency(currency, $"{paramName}[{currency}]"));
        }

        return result;
    }

    /// <summary>
    /// Gets a user-friendly error message for excluded currencies
    /// </summary>
    /// <param name="currencyCode">The excluded currency code</param>
    /// <returns>Error message</returns>
    public static string GetExclusionErrorMessage(string currencyCode)
    {
        return $"Currency '{currencyCode?.ToUpper()}' is not supported. The following currencies are excluded: {string.Join(", ", ExcludedCurrencies)}.";
    }
}
