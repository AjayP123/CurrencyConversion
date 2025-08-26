using System.ComponentModel.DataAnnotations;

namespace CurrencyConversionApi.DTOs;

/// <summary>
/// Request DTO for currency conversion
/// </summary>
public class ConversionRequestDto
{
    /// <summary>
    /// Amount to convert
    /// </summary>
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Source currency code (ISO 4217)
    /// </summary>
    [Required]
    [StringLength(3, MinimumLength = 3, ErrorMessage = "Currency code must be exactly 3 characters")]
    [RegularExpression(@"^[A-Z]{3}$", ErrorMessage = "Currency code must be 3 uppercase letters")]
    public required string FromCurrency { get; set; }

    /// <summary>
    /// Target currency code (ISO 4217)
    /// </summary>
    [Required]
    [StringLength(3, MinimumLength = 3, ErrorMessage = "Currency code must be exactly 3 characters")]
    [RegularExpression(@"^[A-Z]{3}$", ErrorMessage = "Currency code must be 3 uppercase letters")]
    public required string ToCurrency { get; set; }
}
