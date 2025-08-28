# Currency Conversion API

A high-performance, scalable currency conversion API built with ASP.NET Core 9.0, featuring multiple exchange rate providers, smart caching, JWT authentication, and comprehensive monitoring capabilities.

## 🚀 Features

### Core Functionality
- **Latest Exchange Rates**: Get current rates for any base currency
- **Historical Rates**: Access historical exchange rate data
- **Time Series Data**: Retrieve exchange rate trends over time periods
- **Currency Support Check**: Validate if currencies are supported

### Security & Authentication
- **JWT Authentication**: Secure API access with role-based authorization
- **Role-Based Access Control**: Basic, Premium, and Admin user tiers
- **Request Correlation**: Unique correlation IDs for request tracking

### Performance & Reliability
- **Multi-Provider Architecture**: Automatic failover between exchange rate providers
- **Smart Caching**: Business-hours aware caching with optimal TTL
- **Circuit Breaker Pattern**: Resilient external API calls with Polly
- **Cross-Platform Support**: Runs on Windows, Linux, and Docker

## 🏗️ Design Patterns

### 1. **Factory Pattern**
```csharp
public interface IExchangeRateProviderFactory
{
    IExchangeRateProvider GetActiveProvider();
    IEnumerable<IExchangeRateProvider> GetAllProviders();
}
```
- Dynamic provider selection based on configuration
- Easy addition of new exchange rate providers
- Runtime provider switching without code changes

### 2. **Repository Pattern**
- Clean separation between business logic and data access
- Consistent interface for data operations
- Easy testing with mock repositories

### 3. **Strategy Pattern**
```csharp
public class SmartCacheConfig
{
    public TimeSpan GetOptimalTTL(DateTime? rateDate = null)
    {
        // Business hours: 15 minutes
        // Off hours: 2 hours
        // Near update time: Dynamic calculation
    }
}
```
- Different caching strategies based on context
- Business-hours aware cache TTL
- Configurable cache behavior per environment

### 4. **Circuit Breaker Pattern**
```csharp
services.AddHttpClient<TProvider>()
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy());
```
- Automatic failure detection and recovery
- Prevents cascading failures
- Configurable retry policies with exponential backoff

### 5. **Decorator Pattern**
- Request/Response logging middleware
- Exception handling middleware
- Correlation ID injection

### 6. **Observer Pattern**
- Comprehensive logging with Serilog
- Health check monitoring
- Performance metrics collection

## 🔧 Technical Stack

### Backend
- **Framework**: ASP.NET Core 9.0
- **Authentication**: JWT Bearer tokens
- **Validation**: FluentValidation
- **Caching**: IMemoryCache with smart TTL
- **Resilience**: Polly (Circuit Breaker, Retry, Timeout)
- **Logging**: Serilog with structured logging
- **Documentation**: Swagger/OpenAPI

### Data Sources
- **Primary**: [Frankfurter API](https://www.frankfurter.app/) (Free, EU Central Bank data)

### Infrastructure
- **Containerization**: Docker with Alpine Linux
- **Cross-Platform**: Windows, Linux, macOS
- **Environment Support**: Development, Test, Staging, Production
- **Health Checks**: Built-in endpoint monitoring

## 🚫 Currency Exclusions

The following currencies are **excluded** and will return `HTTP 400 Bad Request`:
- **TRY** (Turkish Lira)
- **PLN** (Polish Zloty) 
- **THB** (Thai Baht)
- **MXN** (Mexican Peso)

This applies to all endpoints including conversion, latest rates, historical data, and time series.

## 🔐 Authentication & Authorization

### User Roles
- **Basic Users**: Currency conversion, latest rates
- **Premium Users**: All Basic features + historical data
- **Admin Users**: All features + time series data + user management


## 📦 Installation & Setup

### Prerequisites
- .NET 9.0 SDK
- Docker (optional)
- Git

### Clone Repository
```bash
git clone https://github.com/AjayP123/CurrencyConversion.git
cd CurrencyConversion
```

## 🚀 Running the Application

### Option 1: Normal Mode (.NET CLI)

#### Development Environment
```bash
# Run with hot reload
dotnet watch run --project CurrencyConversionApi

# Or standard run
dotnet run --project CurrencyConversionApi --environment Development
```

#### Other Environments
```bash
# Test environment
dotnet run --project CurrencyConversionApi --environment Test

# Staging environment  
dotnet run --project CurrencyConversionApi --environment Staging

# Production environment
dotnet run --project CurrencyConversionApi --environment Production
```

### Option 2: Docker Mode

#### Using Docker Compose (Recommended)
```bash
# Development mode
docker-compose up -d

# Production mode
docker-compose -f docker-compose.prod.yml up -d

# View logs
docker-compose logs -f currency-api

# Stop containers
docker-compose down
```

#### Using Docker Commands
```bash
# Build image
docker build -t currency-api .

# Run container
docker run -d \
  --name currency-api \
  -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e JWT_SECRET_KEY="your-jwt-secret-key" \
  currency-api

# View logs
docker logs -f currency-api

# Stop container
docker stop currency-api && docker rm currency-api
```


## 🧪 Testing

### Run All Tests
```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults

# Run specific test category
dotnet test --filter "Category=Integration"
```

### Test Categories
- **Unit Tests**: Service logic, validation, utilities
Coverage Report
<img width="2412" height="1374" alt="image" src="https://github.com/user-attachments/assets/79e3258a-0ccd-440c-a89f-34b8052b032a" />



## 🔍 Monitoring & Observability

### Logging
- **Framework**: Serilog with structured logging
- **Levels**: Debug (Dev) → Information (Test/Staging) → Warning (Prod)
- **Outputs**: Console, File, Application Insights (optional)
- **Correlation**: Unique request IDs for tracing


## 🚀 Extensibility

### Adding New Exchange Rate Providers

1. **Create Provider Class**:
```csharp
public class NewProviderService : IExchangeRateProvider
{
    public async Task<IEnumerable<ExchangeRate>> GetLatestRatesAsync(string baseCurrency)
    {
        // Implementation
    }
}
```

2. **Register in DI Container**:
```csharp
services.AddHttpClient<NewProviderService>()
    .AddHttpClientWithResilience<NewProviderService>(retryCount, circuitBreakerCount);
services.AddScoped<IExchangeRateProvider, NewProviderService>();
```

3. **Update Configuration**:
```json
{
  "ExchangeRates": {
    "ActiveProvider": "NewProvider",
    "Providers": {
      "NewProvider": {
        "BaseUrl": "https://api.newprovider.com/",
        "ApiKey": "your-key",
        "Enabled": true,
        "Priority": 1
      }
    }
  }
}
```


### Adding New Caching Strategies

1. **Implement ICacheService**:
```csharp
public class RedisCacheService : ICacheService
{
    public async Task<ExchangeRate?> GetExchangeRateAsync(string from, string to)
    {
        // Redis implementation
    }
}
```

2. **Register in DI**:
```csharp
services.AddSingleton<ICacheService, RedisCacheService>();
```

## 📈 Performance Optimization

### Caching Strategy
- **Business Hours**: 15-minute TTL (active trading)
- **Off Hours**: 2-hour TTL (markets closed)
- **Near Update Time**: Dynamic TTL until next provider update
- **Cross-Rate Calculation**: Cached base currency rates for efficiency

### Connection Pooling
```csharp
services.AddHttpClient<Provider>()
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
    {
        MaxConnectionsPerServer = 10
    });
```

### Memory Management
- Scoped service lifetimes for request-specific data
- Singleton services for expensive-to-create objects
- IMemoryCache with size limits and eviction

## 🔒 Security Best Practices

### Implemented Security Features
- ✅ JWT token validation with secure secrets
- ✅ Role-based authorization
- ✅ Input validation and sanitization
- ✅ Correlation ID injection for audit trails
- ✅ Secure headers middleware
- ✅ Exception handling without information disclosure


## 📊 Deployment

### AWS ECS Fargate Production Strategy

#### Architecture Vision
```
Internet → AWS WAF → ALB → ECS Fargate → CloudWatch
        (Rate Limit) (Load Balance) (Auto Scale) (Monitor)
```

### Deployment Approach

**Phase 1: Infrastructure Setup**
- Set up VPC, subnets, and security groups
- Create Application Load Balancer
- Configure ECS cluster and service definitions

**Phase 2: Container Deployment**
- Build and push Docker images to ECR
- Deploy containers with auto-scaling policies
- Configure health checks and monitoring

**Phase 3: Production **
- Enable WAF rate limiting rules
- Set up CloudWatch dashboards
- Configure alerts and automated responses

