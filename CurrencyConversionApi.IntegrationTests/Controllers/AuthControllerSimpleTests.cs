using CurrencyConversionApi.DTOs;
using CurrencyConversionApi.IntegrationTests.Infrastructure;
using CurrencyConversionApi.Models;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace CurrencyConversionApi.IntegrationTests.Controllers;

public class AuthControllerSimpleTests : BaseIntegrationTest
{
    public AuthControllerSimpleTests(IntegrationTestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Login_WithTestCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Username = "testuser",
            Password = "testpass"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        // Since we don't have real user data in integration tests, 
        // any login attempt should return Unauthorized
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Username = "invalid",
            Password = "wrong"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        // Invalid credentials should return Unauthorized, not BadRequest
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AuthenticatedEndpoint_WithoutToken_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/api/v1/currency/currencies");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
