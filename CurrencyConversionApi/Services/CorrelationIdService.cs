namespace CurrencyConversionApi.Services;

/// <summary>
/// Service to provide the current request's correlation ID
/// </summary>
public interface ICorrelationIdService
{
    /// <summary>
    /// Gets the correlation ID for the current request
    /// </summary>
    string GetCorrelationId();
}

/// <summary>
/// Implementation of correlation ID service using HttpContext
/// </summary>
public class CorrelationIdService : ICorrelationIdService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CorrelationIdService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string GetCorrelationId()
    {
        var context = _httpContextAccessor.HttpContext;
        
        // Try to get from HttpContext.Items first (set by middleware)
        if (context?.Items.TryGetValue("CorrelationId", out var correlationId) == true)
        {
            return correlationId?.ToString() ?? Guid.NewGuid().ToString();
        }

        // Fallback: try to get from response headers
        if (context?.Response.Headers.TryGetValue("X-Correlation-ID", out var headerValue) == true)
        {
            return headerValue.FirstOrDefault() ?? Guid.NewGuid().ToString();
        }

        // Last resort: generate new one
        return Guid.NewGuid().ToString();
    }
}
