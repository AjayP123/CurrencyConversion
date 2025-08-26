using CurrencyConversionApi.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace CurrencyConversionApi.Tests.Services;

public class CorrelationIdServiceTests
{
    [Fact]
    public void Returns_Item_Value_When_Present()
    {
        var ctx = new DefaultHttpContext();
        ctx.Items["CorrelationId"] = "abc-123";
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns(ctx);
        var svc = new CorrelationIdService(accessor.Object);
        svc.GetCorrelationId().Should().Be("abc-123");
    }

    [Fact]
    public void Returns_Header_When_Item_Missing()
    {
        var ctx = new DefaultHttpContext();
        ctx.Response.Headers["X-Correlation-ID"] = "hdr-456";
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns(ctx);
        var svc = new CorrelationIdService(accessor.Object);
        svc.GetCorrelationId().Should().Be("hdr-456");
    }

    [Fact]
    public void Generates_New_When_None_Present()
    {
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns(new DefaultHttpContext());
        var svc = new CorrelationIdService(accessor.Object);
        var id = svc.GetCorrelationId();
        id.Should().NotBeNullOrWhiteSpace();
    }
}
