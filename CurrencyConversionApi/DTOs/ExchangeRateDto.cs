namespace CurrencyConversionApi.DTOs;

/// <summary>
/// Request DTO for retrieving latest exchange rates
/// </summary>
public class LatestRatesRequestDto
{
    /// <summary>
    /// Base currency (default: EUR)
    /// </summary>
    public string BaseCurrency { get; set; } = "EUR";

    /// <summary>
    /// Specific currencies to retrieve (optional)
    /// </summary>
    public List<string>? Currencies { get; set; }
}

/// <summary>
/// Request DTO for historical rates with pagination
/// </summary>
public class HistoricalRatesRequestDto
{
    /// <summary>
    /// Start date (YYYY-MM-DD)
    /// </summary>
    public required string StartDate { get; set; }

    /// <summary>
    /// End date (YYYY-MM-DD)
    /// </summary>
    public required string EndDate { get; set; }

    /// <summary>
    /// Base currency (default: EUR)
    /// </summary>
    public string BaseCurrency { get; set; } = "EUR";

    /// <summary>
    /// Page number (1-based)
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; set; } = 10;
}

/// <summary>
/// Pagination information
/// </summary>
public class PaginationDto
{
    /// <summary>
    /// Current page
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total items
    /// </summary>
    public int TotalItems { get; set; }

    /// <summary>
    /// Total pages
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Has next page
    /// </summary>
    public bool HasNext { get; set; }

    /// <summary>
    /// Has previous page
    /// </summary>
    public bool HasPrevious { get; set; }
}
