using CurrencyConversionApi.DTOs;
using System.Net;
using System.Text.Json;

namespace CurrencyConversionApi.Middleware;

/// <summary>
/// Global exception handling middleware
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        
        var response = exception switch
        {
            ArgumentException => CreateErrorResponse(HttpStatusCode.BadRequest, exception.Message),
            InvalidOperationException => CreateErrorResponse(HttpStatusCode.BadRequest, exception.Message),
            UnauthorizedAccessException => CreateErrorResponse(HttpStatusCode.Unauthorized, "Unauthorized access"),
            TimeoutException => CreateErrorResponse(HttpStatusCode.RequestTimeout, "Request timeout"),
            HttpRequestException => CreateErrorResponse(HttpStatusCode.BadGateway, "External service error"),
            _ => CreateErrorResponse(HttpStatusCode.InternalServerError, "An unexpected error occurred")
        };

        context.Response.StatusCode = (int)response.StatusCode;

        var jsonResponse = JsonSerializer.Serialize(response.ApiResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }

    private static (HttpStatusCode StatusCode, ApiResponse<object> ApiResponse) CreateErrorResponse(
        HttpStatusCode statusCode, string message)
    {
        return (statusCode, ApiResponse<object>.CreateError(message, "unknown"));
    }
}
