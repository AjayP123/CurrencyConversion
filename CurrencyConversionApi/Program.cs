using CurrencyConversionApi.Extensions;
using Serilog;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to accept any host (important for Load Balancer)
builder.WebHost.ConfigureKestrel(options =>
{
    options.AllowSynchronousIO = true;
});

// Configure forwarded headers for Load Balancer
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | 
                              Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto |
                              Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedHost;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Override AllowedHosts to accept any host
builder.Configuration["AllowedHosts"] = "*";

// Add services to the container
builder.Services.AddControllers();

// Add application services
builder.Services.AddApplicationServices(builder.Configuration);

// Add Swagger documentation
builder.Services.AddSwaggerDocumentation();

// Add logging
builder.Services.AddLogging(builder.Configuration);

// Add CORS
builder.Services.AddCorsPolicy();

// Replace default logger with Serilog
builder.Host.UseSerilog();

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseApplicationPipeline(app.Environment);

// Map controllers
app.MapControllers();

// Add health check endpoint that bypasses host validation
app.Map("/health", () => 
{
    return Results.Json(new { 
        Status = "Healthy", 
        Timestamp = DateTime.UtcNow,
        Host = "Any host accepted",
        Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
    });
});

// Add simple test endpoint
app.Map("/test", () => Results.Ok("API is working!"));

try
{
    Log.Information("Starting Currency Conversion API");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

[ExcludeFromCodeCoverage]
public partial class ProgramMarker { }
