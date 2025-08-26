using CurrencyConversionApi.Models;

namespace CurrencyConversionApi.Tests.Services;

public class AuthServiceTests
{
	private readonly Mock<IJwtService> _jwtMock = new();
	private readonly Mock<ILogger<AuthService>> _loggerMock = new();
	private readonly AuthService _sut;

	public AuthServiceTests()
	{
		_jwtMock.Setup(j => j.GenerateAccessToken(It.IsAny<User>())).Returns("access-token");
		_jwtMock.Setup(j => j.GenerateRefreshToken()).Returns("refresh-token");
		_jwtMock.Setup(j => j.GetTokenExpiration()).Returns(DateTime.UtcNow.AddMinutes(30));
		_sut = new AuthService(_jwtMock.Object, _loggerMock.Object);
	}

	[Fact]
	public async Task AuthenticateAsync_ReturnsUser_ForValidCredentials()
	{
		var user = await _sut.AuthenticateAsync("admin", "admin123");
		user.Should().NotBeNull();
		user!.Username.Should().Be("admin");
		user.Roles.Should().Contain(UserRoles.Admin);
	}

	[Fact]
	public async Task AuthenticateAsync_ReturnsNull_ForInvalidPassword()
	{
		var user = await _sut.AuthenticateAsync("admin", "wrong");
		user.Should().BeNull();
	}

	[Fact]
	public async Task AuthenticateAsync_ReturnsNull_ForUnknownUser()
	{
		var user = await _sut.AuthenticateAsync("ghost", "whatever");
		user.Should().BeNull();
	}

	[Fact]
	public async Task GenerateTokenAsync_ReturnsTokens()
	{
		var user = await _sut.AuthenticateAsync("admin", "admin123");
		var tokenResponse = await _sut.GenerateTokenAsync(user!);
		tokenResponse.AccessToken.Should().Be("access-token");
		tokenResponse.RefreshToken.Should().Be("refresh-token");
		tokenResponse.Roles.Should().Contain(UserRoles.Admin);
	}

	[Fact]
	public void GetDemoUsers_ReturnsAllUsers()
	{
		var users = _sut.GetDemoUsers();
		users.Should().HaveCountGreaterOrEqualTo(3);
	}
}
