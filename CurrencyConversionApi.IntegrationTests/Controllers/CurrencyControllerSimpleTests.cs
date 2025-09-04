using CurrencyConversionApi.DTOs;
using CurrencyConversionApi.IntegrationTests.Infrastructure;
using CurrencyConversionApi.Models;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace CurrencyConversionApi.IntegrationTests.Controllers;

public class CurrencyControllerSimpleTests : BaseIntegrationTest
{
    public CurrencyControllerSimpleTests(IntegrationTestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task ConvertCurrency_AsBasicUser_WithValidRequest_ReturnsOk()
    {
        // Arrange
        AuthenticateAs("basicuser", UserRoles.Basic);
        var request = new ConversionRequestDto
        {
            FromCurrency = "USD",
            ToCurrency = "EUR",
            Amount = 100m
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/currency/convert", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ConvertCurrency_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var request = new ConversionRequestDto
        {
            FromCurrency = "USD",
            ToCurrency = "EUR",
            Amount = 100m
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/currency/convert", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetSupportedCurrencies_AsAuthenticatedUser_ReturnsOk()
    {
        // Arrange
        AuthenticateAs("basicuser", UserRoles.Basic);

        // Act
        var response = await Client.GetAsync("/api/v1/currency/currencies");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
