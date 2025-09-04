using System.Net;

namespace CurrencyConversionApi.IntegrationTests.TestDoubles;

public class TestHttpClientFactory : IHttpClientFactory
{
    private readonly Dictionary<string, HttpClient> _clients = new();

    public HttpClient CreateClient(string name)
    {
        if (_clients.TryGetValue(name, out var existingClient))
        {
            return existingClient;
        }

        var handler = new TestHttpMessageHandler();
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:9999/"),
            Timeout = TimeSpan.FromSeconds(30)
        };

        _clients[name] = client;
        return client;
    }
}

public class TestHttpMessageHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // This handler is used by the TestExchangeRateProvider
        // For integration tests, we're using the TestExchangeRateProvider directly
        // So this handler just returns a success response
        
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"message\": \"Test response\"}")
        };

        return Task.FromResult(response);
    }
}
