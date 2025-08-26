using CurrencyConversionApi.Interfaces;
using CurrencyConversionApi.Models;
using CurrencyConversionApi.DTOs;
using CurrencyConversionApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace CurrencyConversionApi.Controllers;

/// <summary>
/// Authentication controller for JWT token management
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Tags("Authentication")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICorrelationIdService _correlationService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        ICorrelationIdService correlationService,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _correlationService = correlationService;
        _logger = logger;
    }

    /// <summary>
    /// Authenticate user and receive JWT token
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>JWT token response</returns>
    /// <response code="200">Authentication successful</response>
    /// <response code="400">Invalid request</response>
    /// <response code="401">Authentication failed</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<TokenResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<TokenResponse>>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(ApiResponse<object>.CreateError(
                "Username and password are required", 
                _correlationService.GetCorrelationId()));
        }

        var user = await _authService.AuthenticateAsync(request.Username, request.Password);
        if (user == null)
        {
            return Unauthorized(ApiResponse<object>.CreateError(
                "Invalid username or password", 
                _correlationService.GetCorrelationId()));
        }

        var tokenResponse = await _authService.GenerateTokenAsync(user);

        _logger.LogInformation("User {Username} authenticated successfully", request.Username);

        return Ok(ApiResponse<TokenResponse>.CreateSuccess(
            tokenResponse, 
            _correlationService.GetCorrelationId(), 
            "Authentication successful"));
    }

    /// <summary>
    /// Get demo user credentials for testing
    /// </summary>
    /// <returns>List of demo users</returns>
    /// <response code="200">Demo users retrieved successfully</response>
    [HttpGet("demo-users")]
    [ProducesResponseType(typeof(ApiResponse<List<object>>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<List<object>>> GetDemoUsers()
    {
        var demoUsers = new List<object>
        {
            new { Username = "admin", Password = "admin123", Roles = new[] { UserRoles.Admin } },
            new { Username = "premium", Password = "premium123", Roles = new[] { UserRoles.Premium } },
            new { Username = "basic", Password = "basic123", Roles = new[] { UserRoles.Basic } }
        };

        return Ok(ApiResponse<List<object>>.CreateSuccess(
            demoUsers, 
            _correlationService.GetCorrelationId(), 
            "Demo users for testing"));
    }
}
