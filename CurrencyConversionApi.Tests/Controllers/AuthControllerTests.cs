using CurrencyConversionApi.Controllers;
using CurrencyConversionApi.DTOs;
using Microsoft.AspNetCore.Mvc;
using CurrencyConversionApi.Models;
using CurrencyConversionApi.Services;

namespace CurrencyConversionApi.Tests.Controllers;

public class AuthControllerTests
{
	private readonly Mock<IAuthService> _authService = new();
	private readonly Mock<ILogger<AuthController>> _logger = new();
	private readonly Mock<ICorrelationIdService> _corr = new();
	private readonly AuthController _sut;

	public AuthControllerTests()
	{
	_corr.Setup(c => c.GetCorrelationId()).Returns("test-corr");
	_sut = new AuthController(_authService.Object, _corr.Object, _logger.Object);
	}

	[Fact]
	public async Task Login_ReturnsUnauthorized_WhenUserNotFound()
	{
		_authService.Setup(a => a.AuthenticateAsync("ghost", It.IsAny<string>())).ReturnsAsync((User?)null);
		var result = await _sut.Login(new LoginRequest { Username = "ghost", Password = "x" });
		var objectResult = result.Result as UnauthorizedObjectResult;
		objectResult.Should().NotBeNull();
		(objectResult!.Value as ApiResponse<object>)!.Success.Should().BeFalse();
	}

	[Fact]
	public async Task Login_ReturnsToken_WhenCredentialsValid()
	{
		var user = new User { Id = "1", Username = "admin", Email = "admin@example.com", PasswordHash = "hash", Roles = new List<string>{ UserRoles.Admin } };
		_authService.Setup(a => a.AuthenticateAsync("admin", "pass")).ReturnsAsync(user);
		_authService.Setup(a => a.GenerateTokenAsync(user)).ReturnsAsync(new TokenResponse
		{
			AccessToken = "at",
			RefreshToken = "rt",
			ExpiresAt = DateTime.UtcNow.AddMinutes(10),
			TokenType = "Bearer",
			Roles = user.Roles
		});

		var result = await _sut.Login(new LoginRequest { Username = "admin", Password = "pass" });
		var ok = result.Result as OkObjectResult;
		ok.Should().NotBeNull();
		var response = (ok!.Value as ApiResponse<TokenResponse>)!;
		response.Success.Should().BeTrue();
		response.Data!.AccessToken.Should().Be("at");
	}
}
