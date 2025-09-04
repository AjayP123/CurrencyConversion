using CurrencyConversionApi.DTOs;
using CurrencyConversionApi.IntegrationTests.Infrastructure;
using CurrencyConversionApi.Models;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace CurrencyConversionApi.IntegrationTests.Controllers;

public class CurrencyControllerTests : BaseIntegrationTest
{
    public CurrencyControllerTests(IntegrationTestWebApplicationFactory factory) : base(factory)
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

    #region Invalid Currency Code Tests

    [Fact]
    public async Task ConvertCurrency_WithInvalidFromCurrency_ReturnsInternalServerError()
    {
        // Arrange
        AuthenticateAs("basicuser", UserRoles.Basic);
        var request = new ConversionRequestDto
        {
            FromCurrency = "XXX", // Invalid currency code
            ToCurrency = "EUR",
            Amount = 100m
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/currency/convert", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        
        // Verify error response content
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<object>>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeFalse();
        apiResponse.Message.Should().Be("An unexpected error occurred");
    }

    [Fact]
    public async Task ConvertCurrency_WithInvalidToCurrency_ReturnsInternalServerError()
    {
        // Arrange
        AuthenticateAs("basicuser", UserRoles.Basic);
        var request = new ConversionRequestDto
        {
            FromCurrency = "USD",
            ToCurrency = "ZZZ", // Invalid currency code
            Amount = 100m
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/currency/convert", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        
        // Verify error response content
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<object>>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeFalse();
        apiResponse.Message.Should().Be("An unexpected error occurred");
    }

    #endregion

    #region Amount Validation Tests

    [Fact]
    public async Task ConvertCurrency_WithZeroAmount_ReturnsBadRequest()
    {
        // Arrange
        AuthenticateAs("basicuser", UserRoles.Basic);
        var request = new ConversionRequestDto
        {
            FromCurrency = "USD",
            ToCurrency = "EUR",
            Amount = 0m
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/currency/convert", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task ConvertCurrency_WithNegativeAmount_ReturnsBadRequest()
    {
        // Arrange
        AuthenticateAs("basicuser", UserRoles.Basic);
        var request = new ConversionRequestDto
        {
            FromCurrency = "USD",
            ToCurrency = "EUR",
            Amount = -100m
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/currency/convert", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task ConvertCurrency_WithVeryLargeAmount_ReturnsOk()
    {
        // Arrange
        AuthenticateAs("basicuser", UserRoles.Basic);
        var request = new ConversionRequestDto
        {
            FromCurrency = "USD",
            ToCurrency = "EUR",
            Amount = 999999999.99m
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/currency/convert", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Same Currency Conversion Tests

    [Fact]
    public async Task ConvertCurrency_SameCurrency_ReturnsOk()
    {
        // Arrange
        AuthenticateAs("basicuser", UserRoles.Basic);
        var request = new ConversionRequestDto
        {
            FromCurrency = "USD",
            ToCurrency = "USD",
            Amount = 100m
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/currency/convert", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verify response content
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<ConversionResponseDto>>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Amount.Should().Be(100m);
        apiResponse.Data.ConvertedAmount.Should().Be(100m); // Same currency = 1:1 conversion
        apiResponse.Data.FromCurrency.Should().Be("USD");
        apiResponse.Data.ToCurrency.Should().Be("USD");
    }

    #endregion

    #region Different User Role Tests

    [Fact]
    public async Task ConvertCurrency_AsPremiumUser_ReturnsOk()
    {
        // Arrange
        AuthenticateAs("premiumuser", UserRoles.Premium);
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
    public async Task ConvertCurrency_AsAdminUser_ReturnsOk()
    {
        // Arrange
        AuthenticateAs("adminuser", UserRoles.Admin);
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

    #endregion

    #region Multiple Currency Pair Tests

    [Fact]
    public async Task ConvertCurrency_GBP_To_JPY_ReturnsOk()
    {
        // Arrange
        AuthenticateAs("basicuser", UserRoles.Basic);
        var request = new ConversionRequestDto
        {
            FromCurrency = "GBP",
            ToCurrency = "JPY",
            Amount = 100m
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/currency/convert", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verify conversion rate
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<ConversionResponseDto>>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.ExchangeRate.Should().Be(150.7m); // Expected rate from TestExchangeRateProvider
        apiResponse.Data.ConvertedAmount.Should().Be(15070m); // 100 * 150.7
    }

    [Fact]
    public async Task ConvertCurrency_EUR_To_CHF_ReturnsOk()
    {
        // Arrange
        AuthenticateAs("basicuser", UserRoles.Basic);
        var request = new ConversionRequestDto
        {
            FromCurrency = "EUR",
            ToCurrency = "CHF",
            Amount = 50m
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/currency/convert", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verify conversion rate
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<ConversionResponseDto>>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.ExchangeRate.Should().Be(1.08m); // Expected rate from TestExchangeRateProvider
        apiResponse.Data.ConvertedAmount.Should().Be(54m); // 50 * 1.08
    }

    [Fact]
    public async Task ConvertCurrency_USD_To_CAD_ReturnsOk()
    {
        // Arrange
        AuthenticateAs("basicuser", UserRoles.Basic);
        var request = new ConversionRequestDto
        {
            FromCurrency = "USD",
            ToCurrency = "CAD",
            Amount = 200m
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/currency/convert", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verify conversion rate
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<ConversionResponseDto>>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.ExchangeRate.Should().Be(1.25m); // Expected rate from TestExchangeRateProvider
        apiResponse.Data.ConvertedAmount.Should().Be(250m); // 200 * 1.25
    }

    #endregion

    #region Response Content Validation Tests

    [Fact]
    public async Task ConvertCurrency_ValidResponse_ContainsAllRequiredFields()
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
        
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<ConversionResponseDto>>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        // Verify API response structure
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();

        // Verify all required fields are present
        var data = apiResponse.Data!;
        data.Amount.Should().Be(100m);
        data.FromCurrency.Should().Be("USD");
        data.ToCurrency.Should().Be("EUR");
        data.ConvertedAmount.Should().Be(85m); // 100 * 0.85
        data.ExchangeRate.Should().Be(0.85m);
        data.ConversionTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        data.RateLastUpdated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        data.RequestId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ConvertCurrency_PrecisionHandling_ReturnsAccurateDecimals()
    {
        // Arrange
        AuthenticateAs("basicuser", UserRoles.Basic);
        var request = new ConversionRequestDto
        {
            FromCurrency = "USD",
            ToCurrency = "EUR",
            Amount = 1.00m
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/currency/convert", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<ConversionResponseDto>>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        apiResponse.Should().NotBeNull();
        apiResponse!.Data.Should().NotBeNull();
        apiResponse.Data!.ConvertedAmount.Should().Be(0.85m); // Precise calculation: 1.00 * 0.85
    }

    #endregion

    #region Content Type and Headers Tests

    [Fact]
    public async Task ConvertCurrency_ReturnsJsonContentType()
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
        response.Content.Headers.ContentType?.MediaType.Should().Contain("json");
    }

    [Fact]
    public async Task GetSupportedCurrencies_ReturnsJsonContentType()
    {
        // Arrange
        AuthenticateAs("basicuser", UserRoles.Basic);

        // Act
        var response = await Client.GetAsync("/api/v1/currency/currencies");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Contain("json");
    }

    #endregion
}
