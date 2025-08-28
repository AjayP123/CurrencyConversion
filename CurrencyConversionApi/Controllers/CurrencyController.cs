using Microsoft.AspNetCore.Mvc;
using CurrencyConversionApi.DTOs;
using CurrencyConversionApi.Interfaces;
using CurrencyConversionApi.Services;
using CurrencyConversionApi.Authorization;
using CurrencyConversionApi.Utilities;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;

namespace CurrencyConversionApi.Controllers;

/// <summary>
/// Currency Controller with role-based access control
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize] // Require authentication for all endpoints
public class CurrencyController : ControllerBase
{
    private readonly ICurrencyConversionService _currencyService;
    private readonly ICorrelationIdService _correlationIdService;
    private readonly ILogger<CurrencyController> _logger;

    public CurrencyController(
        ICurrencyConversionService currencyService,
        ICorrelationIdService correlationIdService,
        ILogger<CurrencyController> logger)
    {
        _currencyService = currencyService;
        _correlationIdService = correlationIdService;
        _logger = logger;
    }

    /// <summary>
    /// Convert amount from one currency to another (excludes TRY, PLN, THB, and MXN)
    /// Requires: Basic, Premium, or Admin role
    /// </summary>
    /// <param name="request">Currency conversion request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Currency conversion result</returns>
    /// <response code="200">Currency converted successfully</response>
    /// <response code="400">Invalid request parameters</response>
    /// <response code="401">Unauthorized - JWT token required</response>
    /// <response code="403">Forbidden - Insufficient permissions</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("convert")]
    [AuthenticatedUser] // Basic, Premium, or Admin
    [ProducesResponseType(typeof(ApiResponse<ConversionResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<ConversionResponseDto>>> ConvertCurrency(
        [FromBody] ConversionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Converting {Amount} from {From} to {To}", 
                request.Amount, request.FromCurrency, request.ToCurrency);

            var result = await _currencyService.ConvertAsync(request.Amount, request.FromCurrency, request.ToCurrency, cancellationToken);
            var requestId = _correlationIdService.GetCorrelationId();
            
            var response = new ConversionResponseDto
            {
                Amount = result.Amount,
                FromCurrency = result.FromCurrency,
                ToCurrency = result.ToCurrency,
                ConvertedAmount = result.ConvertedAmount,
                ExchangeRate = result.ExchangeRate,
                ConversionTime = DateTime.UtcNow,
                RateLastUpdated = result.RateLastUpdated,
                RequestId = requestId
            };
            
            return Ok(ApiResponse<ConversionResponseDto>.CreateSuccess(response, requestId));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid currency conversion request");
            var requestId = _correlationIdService.GetCorrelationId();
            return BadRequest(ApiResponse<object>.CreateError(ex.Message, requestId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting currency");
            var requestId = _correlationIdService.GetCorrelationId();
            return StatusCode(500, ApiResponse<object>.CreateError("An unexpected error occurred", requestId));
        }
    }

    /// <summary>
    /// Get latest exchange rates for a specific base currency with optional currency filtering (excludes TRY, PLN, THB, and MXN)
    /// Requires: Premium or Admin role (advanced feature)
    /// </summary>
    /// <param name="base">Base currency code (default: EUR)</param>
    /// <param name="symbols">Comma-separated list of target currencies to include (e.g. "CHF,GBP")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Latest exchange rates</returns>
    /// <response code="200">Latest rates retrieved successfully</response>
    /// <response code="400">Invalid base currency or symbols</response>
    /// <response code="401">Unauthorized - JWT token required</response>
    /// <response code="403">Forbidden - Premium or Admin role required</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("latest")]
    [PremiumOrAdmin] // Premium or Admin only
    [ProducesResponseType(typeof(ApiResponse<LatestRatesResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<LatestRatesResponseDto>>> GetLatestRates(
        [FromQuery] string? @base = "EUR",
        [FromQuery] string? symbols = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate base currency
            if (CurrencyValidationHelper.ExcludedCurrencies.Contains(@base?.ToUpper() ?? ""))
            {
                var correlationId = _correlationIdService.GetCorrelationId();
                return BadRequest(ApiResponse<object>.CreateError(
                    CurrencyValidationHelper.GetExclusionErrorMessage(@base ?? ""), correlationId));
            }

            _logger.LogInformation("Getting latest rates for base currency {BaseCurrency} with symbols filter {Symbols}", @base, symbols);

            // Parse and validate symbols if provided
            List<string>? targetCurrencies = null;
            if (!string.IsNullOrWhiteSpace(symbols))
            {
                var symbolsList = symbols.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim().ToUpper())
                    .ToList();

                // Check for excluded currencies in symbols
                var excludedSymbols = symbolsList.Where(s => CurrencyValidationHelper.ExcludedCurrencies.Contains(s)).ToList();
                if (excludedSymbols.Any())
                {
                    var correlationId = _correlationIdService.GetCorrelationId();
                    return BadRequest(ApiResponse<object>.CreateError(
                        $"The following currencies in symbols are not supported: {string.Join(", ", excludedSymbols)}. Excluded currencies: {string.Join(", ", CurrencyValidationHelper.ExcludedCurrencies)}", correlationId));
                }

                targetCurrencies = symbolsList;
            }

            var rates = await _currencyService.GetLatestRatesAsync(@base, targetCurrencies, cancellationToken);
            var requestId = _correlationIdService.GetCorrelationId();
            
            var ratesList = rates.ToList();
            if (!ratesList.Any())
            {
                return BadRequest(ApiResponse<object>.CreateError($"No rates available for base currency {@base}", requestId));
            }

            var response = new LatestRatesResponseDto
            {
                BaseCurrency = ratesList.First().FromCurrency,
                Date = ratesList.First().LastUpdated,
                Rates = ratesList.ToDictionary(r => r.ToCurrency, r => r.Rate),
                RequestId = requestId,
                Source = ratesList.First().Source
            };
            
            return Ok(ApiResponse<LatestRatesResponseDto>.CreateSuccess(response, requestId));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid latest rates request");
            var requestId = _correlationIdService.GetCorrelationId();
            return BadRequest(ApiResponse<object>.CreateError(ex.Message, requestId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting latest rates");
            var requestId = _correlationIdService.GetCorrelationId();
            return StatusCode(500, ApiResponse<object>.CreateError("An unexpected error occurred", requestId));
        }
    }

    /// <summary>
    /// Get historical exchange rates for a specific date and base currency with optional symbol filtering (excludes TRY, PLN, THB, and MXN)
    /// Requires: Admin role only (premium feature)
    /// </summary>
    /// <param name="date">Historical date in YYYY-MM-DD format</param>
    /// <param name="base">Base currency code (default: EUR)</param>
    /// <param name="symbols">Comma-separated list of target currencies to include (e.g. "CHF,GBP")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Historical exchange rates</returns>
    /// <response code="200">Historical rates retrieved successfully</response>
    /// <response code="400">Invalid date, base currency, or symbols</response>
    /// <response code="401">Unauthorized - JWT token required</response>
    /// <response code="403">Forbidden - Admin role required</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("historical")]
    [AdminOnly] // Admin only
    [ProducesResponseType(typeof(ApiResponse<HistoricalRatesResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<HistoricalRatesResponseDto>>> GetHistoricalRates(
        [FromQuery, Required] string date,
        [FromQuery] string? @base = "EUR",
        [FromQuery] string? symbols = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!DateTime.TryParse(date, out var parsedDate))
            {
                var requestId = _correlationIdService.GetCorrelationId();
                return BadRequest(ApiResponse<object>.CreateError("Invalid date format. Use YYYY-MM-DD", requestId));
            }

            if (parsedDate.Date > DateTime.Today)
            {
                var requestId = _correlationIdService.GetCorrelationId();
                return BadRequest(ApiResponse<object>.CreateError("Date cannot be in the future", requestId));
            }

            // Parse symbols if provided
            List<string>? targetCurrencies = null;
            if (!string.IsNullOrWhiteSpace(symbols))
            {
                targetCurrencies = symbols.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim().ToUpper())
                    .ToList();
            }

            _logger.LogInformation("Getting historical rates for {Date} with base currency {BaseCurrency} and symbols {Symbols}", 
                parsedDate.ToString("yyyy-MM-dd"), @base, symbols);

            var rates = await _currencyService.GetHistoricalRatesAsync(parsedDate, @base, targetCurrencies, cancellationToken);
            var correlationId = _correlationIdService.GetCorrelationId();
            
            var ratesList = rates.ToList();
            if (!ratesList.Any())
            {
                return BadRequest(ApiResponse<object>.CreateError($"No rates available for {parsedDate:yyyy-MM-dd} with base currency {@base}", correlationId));
            }

            var response = new HistoricalRatesResponseDto
            {
                BaseCurrency = ratesList.First().FromCurrency,
                Date = parsedDate,
                Rates = ratesList.ToDictionary(r => r.ToCurrency, r => r.Rate),
                RequestId = correlationId,
                Source = ratesList.First().Source,
                IsWeekend = parsedDate.DayOfWeek == DayOfWeek.Saturday || parsedDate.DayOfWeek == DayOfWeek.Sunday
            };
            
            return Ok(ApiResponse<HistoricalRatesResponseDto>.CreateSuccess(response, correlationId));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid historical rates request");
            var requestId = _correlationIdService.GetCorrelationId();
            return BadRequest(ApiResponse<object>.CreateError(ex.Message, requestId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting historical rates");
            var requestId = _correlationIdService.GetCorrelationId();
            return StatusCode(500, ApiResponse<object>.CreateError("An unexpected error occurred", requestId));
        }
    }

    /// <summary>
    /// Get time series exchange rates for a date range with optional symbol filtering (excludes TRY, PLN, THB, and MXN)
    /// Requires: Admin role only (premium feature)
    /// </summary>
    /// <param name="startDate">Start date in YYYY-MM-DD format</param>
    /// <param name="endDate">End date in YYYY-MM-DD format</param>
    /// <param name="base">Base currency code (default: EUR)</param>
    /// <param name="symbols">Comma-separated list of target currencies to include (e.g. "CHF,GBP")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Time series exchange rates</returns>
    /// <response code="200">Time series rates retrieved successfully</response>
    /// <response code="400">Invalid dates, base currency, or symbols</response>
    /// <response code="401">Unauthorized - JWT token required</response>
    /// <response code="403">Forbidden - Admin role required</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("time-series")]
    [AdminOnly] // Admin only
    [ProducesResponseType(typeof(ApiResponse<TimeSeriesRatesResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<TimeSeriesRatesResponseDto>>> GetTimeSeriesRates(
        [FromQuery, Required] string startDate,
        [FromQuery, Required] string endDate,
        [FromQuery] string? @base = "EUR",
        [FromQuery] string? symbols = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!DateTime.TryParse(startDate, out var parsedStartDate))
            {
                var requestId = _correlationIdService.GetCorrelationId();
                return BadRequest(ApiResponse<object>.CreateError("Invalid start date format. Use YYYY-MM-DD", requestId));
            }

            if (!DateTime.TryParse(endDate, out var parsedEndDate))
            {
                var requestId = _correlationIdService.GetCorrelationId();
                return BadRequest(ApiResponse<object>.CreateError("Invalid end date format. Use YYYY-MM-DD", requestId));
            }

            if (parsedStartDate > parsedEndDate)
            {
                var requestId = _correlationIdService.GetCorrelationId();
                return BadRequest(ApiResponse<object>.CreateError("Start date cannot be after end date", requestId));
            }

            if (parsedEndDate > DateTime.Today)
            {
                var requestId = _correlationIdService.GetCorrelationId();
                return BadRequest(ApiResponse<object>.CreateError("End date cannot be in the future", requestId));
            }

            // Parse symbols if provided
            List<string>? targetCurrencies = null;
            if (!string.IsNullOrWhiteSpace(symbols))
            {
                targetCurrencies = symbols.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim().ToUpper())
                    .ToList();
            }

            _logger.LogInformation("Getting time series rates from {StartDate} to {EndDate} with base currency {BaseCurrency} and symbols {Symbols}", 
                parsedStartDate.ToString("yyyy-MM-dd"), parsedEndDate.ToString("yyyy-MM-dd"), @base, symbols);

            var timeSeriesData = await _currencyService.GetTimeSeriesRatesAsync(parsedStartDate, parsedEndDate, @base, targetCurrencies, cancellationToken);
            var correlationId = _correlationIdService.GetCorrelationId();
            
            if (!timeSeriesData.Any())
            {
                return BadRequest(ApiResponse<object>.CreateError($"No time series data available for {parsedStartDate:yyyy-MM-dd} to {parsedEndDate:yyyy-MM-dd} with base currency {@base}", correlationId));
            }

            var response = new TimeSeriesRatesResponseDto
            {
                BaseCurrency = @base ?? "EUR",
                StartDate = parsedStartDate,
                EndDate = parsedEndDate,
                Rates = timeSeriesData,
                RequestId = correlationId,
                Source = "Frankfurter"
            };
            
            return Ok(ApiResponse<TimeSeriesRatesResponseDto>.CreateSuccess(response, correlationId));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid time series request");
            var requestId = _correlationIdService.GetCorrelationId();
            return BadRequest(ApiResponse<object>.CreateError(ex.Message, requestId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting time series rates");
            var requestId = _correlationIdService.GetCorrelationId();
            return StatusCode(500, ApiResponse<object>.CreateError("An unexpected error occurred", requestId));
        }
    }

    /// <summary>
    /// Get supported currencies list
    /// Requires: Any authenticated user (Basic/Premium/Admin)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of supported currencies</returns>
    /// <response code="200">Currencies retrieved successfully</response>
    /// <response code="401">Unauthorized - JWT token required</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("currencies")]
    [AuthenticatedUser] // All authenticated users
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<string>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<IEnumerable<string>>>> GetSupportedCurrencies(
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching supported currencies (excluding TRY, PLN, THB, MXN)");

            var currencies = await _currencyService.GetSupportedCurrenciesAsync(cancellationToken);
            var requestId = _correlationIdService.GetCorrelationId();
            
            // Filter out excluded currencies
            var currencyCodes = currencies
                .Where(c => !CurrencyValidationHelper.ExcludedCurrencies.Contains(c.Code))
                .Select(c => c.Code);
            
            return Ok(ApiResponse<IEnumerable<string>>.CreateSuccess(currencyCodes, requestId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching supported currencies");
            var requestId = _correlationIdService.GetCorrelationId();
            return StatusCode(500, ApiResponse<object>.CreateError("An unexpected error occurred", requestId));
        }
    }

    /// <summary>
    /// Check if a currency is supported
    /// Requires: Any authenticated user (Basic/Premium/Admin)
    /// </summary>
    /// <param name="currencyCode">Currency code to check (e.g., USD, EUR)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Whether the currency is supported</returns>
    /// <response code="200">Currency support status retrieved successfully</response>
    /// <response code="400">Invalid currency code</response>
    /// <response code="401">Unauthorized - JWT token required</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("currencies/{currencyCode}/supported")]
    [AuthenticatedUser] // All authenticated users
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<bool>>> IsCurrencySupported(
        [FromRoute] string currencyCode,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(currencyCode) || currencyCode.Length != 3)
            {
                var requestId = _correlationIdService.GetCorrelationId();
                return BadRequest(ApiResponse<object>.CreateError("Currency code must be exactly 3 characters", requestId));
            }

            // Check if currency is excluded
            if (CurrencyValidationHelper.ExcludedCurrencies.Contains(currencyCode.ToUpper()))
            {
                var requestId = _correlationIdService.GetCorrelationId();
                return BadRequest(ApiResponse<object>.CreateError(
                    CurrencyValidationHelper.GetExclusionErrorMessage(currencyCode), requestId));
            }

            _logger.LogInformation("Checking if currency {Currency} is supported", currencyCode);

            var isSupported = await _currencyService.IsCurrencySupportedAsync(currencyCode, cancellationToken);
            var correlationId = _correlationIdService.GetCorrelationId();
            
            return Ok(ApiResponse<bool>.CreateSuccess(isSupported, correlationId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking currency support for {Currency}", currencyCode);
            var requestId = _correlationIdService.GetCorrelationId();
            return StatusCode(500, ApiResponse<object>.CreateError("An unexpected error occurred", requestId));
        }
    }
}
