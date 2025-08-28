using CurrencyConversionApi.Configuration;
using CurrencyConversionApi.Interfaces;
using CurrencyConversionApi.Services;
using CurrencyConversionApi.Middleware;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Reflection;
using FluentValidation;
using Polly;
using Polly.Extensions.Http;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Diagnostics.CodeAnalysis;

namespace CurrencyConversionApi.Extensions;

/// <summary>
/// Service collection extensions for dependency injection
/// </summary>
/// <summary>
/// Dependency injection registration helpers (excluded from coverage as infrastructure wiring)
/// </summary>
[ExcludeFromCodeCoverage]
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add all application services
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Configuration
        services.Configure<ExchangeRateConfig>(configuration.GetSection(ExchangeRateConfig.SectionName));
        services.Configure<SmartCacheConfig>(configuration.GetSection("SmartCache"));
        services.Configure<JwtConfig>(configuration.GetSection(JwtConfig.SectionName));

        // JWT Authentication Services
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IAuthService, AuthService>();

        // JWT Authentication Configuration
        var jwtConfig = configuration.GetSection(JwtConfig.SectionName).Get<JwtConfig>()
            ?? throw new InvalidOperationException("JWT configuration is missing");

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig.SecretKey)),
                ValidateIssuer = true,
                ValidIssuer = jwtConfig.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtConfig.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                    {
                        context.Response.Headers["Token-Expired"] = "true";
                    }
                    return Task.CompletedTask;
                }
            };
        });

        // Memory Cache - no size limits, controlled by item count limits instead
        services.AddMemoryCache();

        // Core services following the requirements
        services.AddScoped<ICurrencyConversionService, CurrencyConversionService>();
        
        // Register all exchange rate providers
        services.AddScoped<FrankfurterApiProvider>();
        services.AddScoped<ExchangeRateApiProvider>();
        services.AddScoped<CurrencyApiProvider>();
        
        // Register provider factory
        services.AddScoped<IExchangeRateProviderFactory, ExchangeRateProviderFactory>();
        
        // Choose cache service based on configuration
        // var cacheMode = configuration.GetValue<string>("CacheMode", "optimized");
    
        services.AddSingleton<ICacheService, OptimizedCacheService>();
       
        
        // Request correlation tracking
        services.AddHttpContextAccessor();
        services.AddScoped<ICorrelationIdService, CorrelationIdService>();

        // HTTP Clients with resilience policies for all providers
        AddHttpClientWithResilience<FrankfurterApiProvider>(services, "FrankfurterApi", "https://api.frankfurter.app/", "Frankfurter API");
        AddHttpClientWithResilience<ExchangeRateApiProvider>(services, "ExchangeRateApi", "https://api.exchangerate-api.com/v4/", "ExchangeRate API");
        AddHttpClientWithResilience<CurrencyApiProvider>(services, "CurrencyApi", "https://api.currencyapi.com/v3/", "Currency API");

        // Memory Cache - no size limits, controlled by item count limits instead
        services.AddMemoryCache();

        // Validation
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        return services;
    }

    /// <summary>
    /// Add HTTP client with resilience policies (circuit breaker and retry)
    /// </summary>
    private static void AddHttpClientWithResilience<TProvider>(
        IServiceCollection services, 
        string clientName, 
        string baseUrl, 
        string providerDisplayName) where TProvider : class
    {
        services.AddHttpClient(clientName, client =>
        {
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddPolicyHandler((serviceProvider, request) =>
        {
            var config = serviceProvider.GetRequiredService<IOptions<ExchangeRateConfig>>().Value;
            var logger = serviceProvider.GetRequiredService<ILogger<TProvider>>();
            
            // Circuit breaker policy
            var circuitBreakerPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: 3,
                    durationOfBreak: TimeSpan.FromSeconds(30),
                    onBreak: (result, duration) =>
                    {
                        logger.LogError("Circuit breaker opened for {Provider}. Duration: {Duration}s. Reason: {Reason}", 
                            providerDisplayName, duration.TotalSeconds, result.Exception?.Message ?? "HTTP error");
                    },
                    onReset: () =>
                    {
                        logger.LogInformation("Circuit breaker reset for {Provider} - service recovered", providerDisplayName);
                    },
                    onHalfOpen: () =>
                    {
                        logger.LogWarning("Circuit breaker half-open for {Provider} - testing service recovery", providerDisplayName);
                    });

            // Retry policy with exponential backoff
            var retryPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(
                    config.MaxRetryAttempts,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        logger.LogWarning("Retry {RetryCount} for {Provider} in {Delay}ms", 
                            retryCount, providerDisplayName, timespan.TotalMilliseconds);
                    });

            // Combine policies: Retry first, then circuit breaker
            return Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);
        });
    }

    /// <summary>
    /// Add Swagger/OpenAPI documentation
    /// </summary>
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Currency Conversion API",
                Version = "v1",
                Description = "A robust, scalable currency conversion API built with ASP.NET Core",
                Contact = new OpenApiContact
                {
                    Name = "API Support",
                    Email = "support@currencyapi.com"
                }
            });

            // Include XML comments
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                c.IncludeXmlComments(xmlPath);
            }

            // Add security definitions
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }

    /// <summary>
    /// Add logging with Serilog
    /// </summary>
    public static IServiceCollection AddLogging(this IServiceCollection services, IConfiguration configuration)
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .CreateLogger();

        services.AddSerilog();

        return services;
    }

    /// <summary>
    /// Add CORS policy
    /// </summary>
    public static IServiceCollection AddCorsPolicy(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("DefaultPolicy", policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
        });

        return services;
    }
}

/// <summary>
/// Application builder extensions for middleware pipeline
/// </summary>
/// <summary>
/// Middleware pipeline helpers (excluded from coverage as infrastructure wiring)
/// </summary>
[ExcludeFromCodeCoverage]
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Configure the application pipeline
    /// </summary>
    public static IApplicationBuilder UseApplicationPipeline(this IApplicationBuilder app, IWebHostEnvironment env)
    {
        // Exception handling
        app.UseMiddleware<ExceptionHandlingMiddleware>();

        // Request logging
        app.UseMiddleware<RequestLoggingMiddleware>();
        // Development specific middleware
        if (env.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Currency Conversion API v1");
                c.RoutePrefix = string.Empty; // Serve Swagger at root
            });
        }

        // Security headers
        app.Use(async (context, next) =>
        {
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["X-Frame-Options"] = "DENY";
            context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
            context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            await next();
        });

        // Standard middleware
        app.UseHttpsRedirection();
        app.UseCors("DefaultPolicy");
        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }
}
