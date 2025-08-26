using Serilog.Context;

namespace CurrencyConversionApi.Middleware;

/// <summary>
/// Middleware to add request correlation ID for tracing
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Generate correlation ID
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() 
            ?? Guid.NewGuid().ToString();

        context.Items["CorrelationId"] = correlationId;
        context.Response.Headers["X-Correlation-ID"] = correlationId;

        // Add to Serilog context
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            var startTime = DateTime.UtcNow;
            
            _logger.LogInformation("Starting request {Method} {Path}", 
                context.Request.Method, context.Request.Path);

            try
            {
                await _next(context);
            }
            finally
            {
                var duration = DateTime.UtcNow - startTime;
                
                _logger.LogInformation("Completed request {Method} {Path} with status {StatusCode} in {Duration}ms",
                    context.Request.Method, 
                    context.Request.Path, 
                    context.Response.StatusCode, 
                    duration.TotalMilliseconds);
            }
        }
    }
}
