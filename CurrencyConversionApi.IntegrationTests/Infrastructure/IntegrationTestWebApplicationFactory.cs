using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using CurrencyConversionApi.Services;
using CurrencyConversionApi.Interfaces;
using CurrencyConversionApi.IntegrationTests.TestDoubles;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using CurrencyConversionApi.Models;
using CurrencyConversionApi.Configuration;

namespace CurrencyConversionApi.IntegrationTests.Infrastructure;

public class IntegrationTestWebApplicationFactory : WebApplicationFactory<ProgramMarker>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Clear existing configuration
            config.Sources.Clear();
            
            // Add integration test configuration
            config.AddJsonFile("appsettings.IntegrationTest.json", optional: false);
            config.AddEnvironmentVariables();
        });

        builder.ConfigureServices(services =>
        {
            // Remove the existing IExchangeRateProvider registrations
            var descriptors = services.Where(d => d.ServiceType == typeof(IExchangeRateProvider)).ToList();
            foreach (var descriptor in descriptors)
            {
                services.Remove(descriptor);
            }

            // Remove the existing IExchangeRateProviderFactory registration
            services.RemoveAll<IExchangeRateProviderFactory>();

            // Remove the existing HttpClient factory registration
            services.RemoveAll<IHttpClientFactory>();
            services.RemoveAll<HttpClient>();

            // Register test doubles
            services.AddSingleton<IHttpClientFactory, TestHttpClientFactory>();
            services.AddScoped<TestExchangeRateProvider>();
            services.AddScoped<IExchangeRateProvider>(provider => provider.GetRequiredService<TestExchangeRateProvider>());
            services.AddScoped<IExchangeRateProviderFactory, TestExchangeRateProviderFactory>();

            // Override JWT configuration for testing
            services.Configure<JwtConfig>(options =>
            {
                options.SecretKey = "integration-test-secret-key-for-jwt-testing-purposes-minimum-256-bits-required";
                options.Issuer = "IntegrationTestIssuer";
                options.Audience = "IntegrationTestAudience";
                options.ExpirationMinutes = 60;
                options.RefreshTokenExpirationDays = 1;
            });
        });

        builder.UseEnvironment("IntegrationTest");
    }

    public string GenerateTestToken(string username, string role)
    {
        var serviceProvider = Services;
        var jwtService = serviceProvider.GetRequiredService<IJwtService>();
        
        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            Username = username,
            Email = $"{username}@test.com",
            PasswordHash = "test-hash",
            Roles = new List<string> { role }
        };

        return jwtService.GenerateAccessToken(user);
    }
}
