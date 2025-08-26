namespace CurrencyConversionApi.DTOs;

/// <summary>
/// Standard API response wrapper
/// </summary>
public class ApiResponse<T>
{
    /// <summary>
    /// Indicates if the request was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Response data
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Error message if request failed
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Additional error details
    /// </summary>
    public object? Errors { get; set; }

    /// <summary>
    /// Request timestamp
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Request ID for tracing
    /// </summary>
    public required string RequestId { get; set; }

    /// <summary>
    /// Create a successful response
    /// </summary>
    public static ApiResponse<T> CreateSuccess(T data, string requestId, string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message,
            RequestId = requestId
        };
    }

    /// <summary>
    /// Create an error response
    /// </summary>
    public static ApiResponse<T> CreateError(string message, string requestId, object? errors = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Errors = errors,
            RequestId = requestId
        };
    }
}
