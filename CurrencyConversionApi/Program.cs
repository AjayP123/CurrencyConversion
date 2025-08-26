using CurrencyConversionApi.Extensions;
using Serilog;
using System.Diagnostics.CodeAnalysis;

var builder = WebApplication.CreateBuilder(args);

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

// Add health check endpoint
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }));

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
