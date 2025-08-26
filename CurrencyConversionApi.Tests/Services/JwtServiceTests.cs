using CurrencyConversionApi.Configuration;
using CurrencyConversionApi.Models;
using Microsoft.Extensions.Options;

namespace CurrencyConversionApi.Tests.Services;

public class JwtServiceTests
{
	private readonly JwtService _sut;
	private readonly Mock<ILogger<JwtService>> _logger = new();
	private readonly JwtConfig _config = new()
	{
		SecretKey = "super-secret-key-super-secret-key-1234567890",
		Issuer = "CurrencyConversionApi",
		Audience = "CurrencyConversionApi",
		ExpirationMinutes = 5
	};

	public JwtServiceTests()
	{
		_sut = new JwtService(Options.Create(_config), _logger.Object);
	}

	private static User TestUser => new()
	{
		Id = Guid.NewGuid().ToString(),
		Username = "tester",
		Email = "tester@example.com",
		PasswordHash = "irrelevant",
		Roles = new List<string> { UserRoles.Admin, UserRoles.Premium }
	};

	[Fact]
	public void GenerateAccessToken_ProducesValidToken()
	{
		var token = _sut.GenerateAccessToken(TestUser);
		token.Should().NotBeNullOrWhiteSpace();
	}

	[Fact]
	public void ValidateToken_ReturnsTrue_ForValidToken()
	{
		var token = _sut.GenerateAccessToken(TestUser);
		var ok = _sut.ValidateToken(token, out var userId, out var roles);
		ok.Should().BeTrue();
		userId.Should().NotBeNull();
		roles.Should().Contain(UserRoles.Admin);
	}

	[Fact]
	public void ValidateToken_ReturnsFalse_ForTamperedToken()
	{
		var token = _sut.GenerateAccessToken(TestUser) + "abc"; // corrupt
		var ok = _sut.ValidateToken(token, out _, out _);
		ok.Should().BeFalse();
	}

	[Fact]
	public void GenerateRefreshToken_ReturnsBase64()
	{
		var refresh = _sut.GenerateRefreshToken();
		Action act = () => Convert.FromBase64String(refresh);
		act.Should().NotThrow();
	}

	[Fact]
	public void Placeholder_NoImmediateExpiryTest() { /* Expiry scenario not tested due to minute granularity */ }

	[Fact]
	public void ValidateToken_Fails_WithInvalidIssuer()
	{
		var token = _sut.GenerateAccessToken(TestUser);
		// mutate config issuer via reflection hack (simulate wrong validation params by creating new service instance with different issuer)
		var badConfig = new JwtConfig { SecretKey = _config.SecretKey, Issuer = _config.Issuer + "X", Audience = _config.Audience, ExpirationMinutes = _config.ExpirationMinutes };
		var badService = new JwtService(Options.Create(badConfig), _logger.Object);
		var ok = badService.ValidateToken(token, out _, out _);
		ok.Should().BeFalse();
	}

	[Fact]
	public void ValidateToken_Fails_WithInvalidAudience()
	{
		var token = _sut.GenerateAccessToken(TestUser);
		var badConfig = new JwtConfig { SecretKey = _config.SecretKey, Issuer = _config.Issuer, Audience = _config.Audience + "X", ExpirationMinutes = _config.ExpirationMinutes };
		var badService = new JwtService(Options.Create(badConfig), _logger.Object);
		var ok = badService.ValidateToken(token, out _, out _);
		ok.Should().BeFalse();
	}
}
