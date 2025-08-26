using CurrencyConversionApi.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;

namespace CurrencyConversionApi.Tests.Middleware;

public class MiddlewareTests
{
    [Fact]
    public async Task ExceptionHandlingMiddleware_InvalidOperation_Returns400()
    {
        var logger = new Mock<ILogger<ExceptionHandlingMiddleware>>();
        RequestDelegate next = _ => throw new InvalidOperationException("bad state");
        var mw = new ExceptionHandlingMiddleware(next, logger.Object);
        var ctx = new DefaultHttpContext();
        await mw.InvokeAsync(ctx);
        ctx.Response.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task ExceptionHandlingMiddleware_GenericException_Returns500()
    {
        var logger = new Mock<ILogger<ExceptionHandlingMiddleware>>();
        RequestDelegate next = _ => throw new Exception("boom");
        var mw = new ExceptionHandlingMiddleware(next, logger.Object);
        var ctx = new DefaultHttpContext();
        await mw.InvokeAsync(ctx);
        ctx.Response.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task RequestLoggingMiddleware_AddsCorrelationId()
    {
        var logger = new Mock<ILogger<RequestLoggingMiddleware>>();
        RequestDelegate next = _ => Task.CompletedTask;
        var mw = new RequestLoggingMiddleware(next, logger.Object);
        var ctx = new DefaultHttpContext();
        await mw.InvokeAsync(ctx);
        ctx.Response.Headers.ContainsKey("X-Correlation-ID").Should().BeTrue();
        ctx.Items.ContainsKey("CorrelationId").Should().BeTrue();
    }

    [Theory]
    [InlineData(typeof(ArgumentException), 400)]
    [InlineData(typeof(UnauthorizedAccessException), 401)]
    [InlineData(typeof(TimeoutException), 408)]
    [InlineData(typeof(HttpRequestException), 502)]
    public async Task ExceptionHandlingMiddleware_Maps_StatusCodes(Type exType, int expected)
    {
        var logger = new Mock<ILogger<ExceptionHandlingMiddleware>>();
        RequestDelegate next = _ => throw (Exception)Activator.CreateInstance(exType, "msg")!;
        var mw = new ExceptionHandlingMiddleware(next, logger.Object);
        var ctx = new DefaultHttpContext();
        await mw.InvokeAsync(ctx);
        ctx.Response.StatusCode.Should().Be(expected);
    }
}
