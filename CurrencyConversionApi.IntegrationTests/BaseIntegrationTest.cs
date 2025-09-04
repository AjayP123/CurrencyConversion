using CurrencyConversionApi.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;
using CurrencyConversionApi.Models;
using Xunit;

namespace CurrencyConversionApi.IntegrationTests;

public abstract class BaseIntegrationTest : IClassFixture<IntegrationTestWebApplicationFactory>, IDisposable
{
    protected readonly IntegrationTestWebApplicationFactory Factory;
    protected readonly HttpClient Client;

    protected BaseIntegrationTest(IntegrationTestWebApplicationFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    protected void AuthenticateAs(string username, string role)
    {
        var token = Factory.GenerateTestToken(username, role);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    protected void ClearAuthentication()
    {
        Client.DefaultRequestHeaders.Authorization = null;
    }

    protected T GetService<T>() where T : notnull
    {
        return Factory.Services.GetRequiredService<T>();
    }

    public void Dispose()
    {
        Client?.Dispose();
    }
}
