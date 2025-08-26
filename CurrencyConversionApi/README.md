# Currency Conversion API

A robust, scalable, and maintainable currency conversion API built with C# and ASP.NET Core, designed for high performance, security, and resilience. This API integrates with the **Frankfurter API** to provide real-time and historical exchange rates.

## 🚀 Features & Requirements Implementation

### 1. Endpoints

#### 1.1 Retrieve Latest Exchange Rates
- **Endpoint**: `GET /api/v1/currency/latest?baseCurrency=EUR&currencies=USD,GBP`
- **Description**: Fetch the latest exchange rates for a specific base currency (default: EUR)
- **Features**: Supports filtering by specific currencies, excludes TRY, PLN, THB, MXN

#### 1.2 Currency Conversion  
- **Endpoint**: `POST /api/v1/currency/convert`
- **Description**: Convert amounts between different currencies
- **Validation**: Returns BadRequest (400) if TRY, PLN, THB, or MXN currencies are involved
- **Body Example**:
```json
{
  "amount": 100.50,
  "fromCurrency": "USD",
  "toCurrency": "EUR"
}
```

#### 1.3 Historical Exchange Rates with Pagination
- **Endpoint**: `GET /api/v1/currency/historical?startDate=2020-01-01&endDate=2020-01-31&baseCurrency=EUR&page=1&pageSize=10`
- **Description**: Retrieve historical exchange rates for a given period with pagination
- **Features**: Date range validation, pagination metadata, excludes forbidden currencies

### 2. API Architecture & Design

#### 2.1 Resilience & Performance ✅
- **Caching**: In-memory caching implemented to minimize Frankfurter API calls
- **Retry Policies**: Exponential backoff retry policies using Polly library
- **Circuit Breaker**: Ready for implementation with Polly extensions
- **Performance**: Async/await throughout, connection pooling

#### 2.2 Extensibility & Maintainability ✅
- **Dependency Injection**: Full DI implementation with service abstractions
- **Factory Pattern**: Provider pattern for exchange rate sources (ready for multiple providers)
- **Clean Architecture**: Separated concerns with Controllers, Services, DTOs, Models
- **Future-Ready**: Designed to support multiple exchange rate providers

#### 2.3 Security & Access Control 🔄
- **JWT Authentication**: Framework ready (to be implemented)
- **RBAC**: Role-based access control ready for implementation
- **API Throttling**: Built-in rate limiting and security headers
- **Validation**: Comprehensive input validation with FluentValidation

#### 2.4 Logging & Monitoring ✅
- **Structured Logging**: Serilog with correlation IDs
- **Request Details**: Logs client IP, HTTP method, endpoint, response code, response time
- **Correlation**: Request correlation IDs for debugging
- **File & Console**: Dual logging outputs with different formats

## 📋 API Documentation

### Core Endpoints

#### Get Latest Exchange Rates
```http
GET /api/v1/currency/latest?baseCurrency=EUR&currencies=USD,GBP,JPY
```

**Response:**
```json
{
  "success": true,
  "data": {
    "base": "EUR",
    "date": "2025-08-23",
    "rates": {
      "USD": 1.0853,
      "GBP": 0.8472,
      "JPY": 159.45
    }
  },
  "timestamp": "2025-08-23T10:00:00Z",
  "requestId": "guid"
}
```

#### Convert Currency
```http
POST /api/v1/currency/convert
Content-Type: application/json

{
  "amount": 100,
  "fromCurrency": "USD",
  "toCurrency": "EUR"
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "amount": 100,
    "fromCurrency": "USD",
    "toCurrency": "EUR",
    "convertedAmount": 92.14,
    "exchangeRate": 0.9214,
    "conversionTime": "2025-08-23T10:00:00Z",
    "rateLastUpdated": "2025-08-23T09:45:00Z",
    "requestId": "guid"
  }
}
```

#### Get Historical Rates
```http
GET /api/v1/currency/historical?startDate=2020-01-01&endDate=2020-01-31&baseCurrency=EUR&page=1&pageSize=10
```

**Response:**
```json
{
  "success": true,
  "data": {
    "base": "EUR",
    "startDate": "2020-01-01",
    "endDate": "2020-01-31",
    "rates": {
      "2020-01-01": { "USD": 1.12, "GBP": 0.85 },
      "2020-01-02": { "USD": 1.11, "GBP": 0.84 }
    },
    "pagination": {
      "page": 1,
      "pageSize": 10,
      "totalItems": 31,
      "totalPages": 4,
      "hasNext": true,
      "hasPrevious": false
    }
  }
}
```

### Excluded Currencies
**❌ Not Supported:** TRY, PLN, THB, MXN (returns 400 Bad Request)

### Supported Currencies
**✅ Supported:** EUR, USD, GBP, JPY, AUD, CAD, CHF, CNY, SEK, NOK, NZD, KRW, SGD, HKD, INR, BRL, ZAR, CZK, HUF, BGN, RON, HRK, RUB, ISK, PHP, IDR, MYR

## 🛠️ Installation & Setup

### Prerequisites
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Visual Studio 2022 or VS Code

### Quick Start
```bash
# Clone and navigate to project
cd CurrencyConversionApi

# Restore dependencies
dotnet restore

# Run the application
dotnet run

# API will be available at:
# - https://localhost:7001 (HTTPS)
# - http://localhost:5001 (HTTP)
# - Swagger UI at root URL
```

## ⚙️ Configuration

### appsettings.json
```json
{
  "ExchangeRates": {
    "PrimaryProvider": "Frankfurter",
    "CacheDuration": "00:15:00",
    "RequestTimeout": "00:00:10",
    "MaxRetryAttempts": 3
  },
  "Cache": {
    "Type": "Memory",
    "DefaultExpiry": "00:15:00",
    "MemoryCacheSizeLimitMB": 100
  }
}
```

## 🏗️ Architecture

### Project Structure
```
├── Controllers/           # API endpoints (requirements-based)
├── Services/             # Business logic & Frankfurter integration
├── Interfaces/           # Service contracts
├── DTOs/                # Request/Response models
├── Models/              # Domain models
├── Configuration/       # Settings classes
├── Middleware/          # Request pipeline components
├── Extensions/          # DI & pipeline configuration
├── Validators/          # Input validation (excludes forbidden currencies)
└── Infrastructure/      # External integrations
```

### Key Components

#### ExchangeRateService
- Implements the three required endpoints
- Integrates with Frankfurter API
- Handles currency exclusion (TRY, PLN, THB, MXN)
- Includes caching and error handling

#### FrankfurterApiProvider
- Dedicated Frankfurter API integration
- Retry policies with exponential backoff
- Circuit breaker ready
- Comprehensive error handling

#### Validation Layer
- FluentValidation for request validation
- Automatic rejection of excluded currencies
- Input sanitization and bounds checking

## 🧪 Testing Strategy

### Coverage Goals
- **Target**: 90%+ unit test coverage
- **Integration Tests**: API endpoint testing
- **Test Coverage Reports**: Built-in support

### Test Structure (Ready for Implementation)
```
Tests/
├── Unit/
│   ├── Controllers/
│   ├── Services/
│   └── Validators/
├── Integration/
│   ├── ApiTests/
│   └── FrankfurterIntegration/
└── Coverage/
```

## 🚀 Deployment & Scalability

### Multi-Environment Support
- **Dev**: Extended timeouts, verbose logging
- **Test**: Production-like configuration
- **Prod**: Optimized caching, minimal logging

### Horizontal Scaling
- Stateless design
- External cache ready (Redis)
- Load balancer compatible

### API Versioning
- URL-based versioning (`/api/v1/`)
- Future-proof design
- Backward compatibility

## 📊 Monitoring & Observability

### Structured Logging
```json
{
  "timestamp": "2025-08-23T10:00:00Z",
  "level": "Information", 
  "correlationId": "abc-123",
  "message": "Converting 100 USD to EUR",
  "clientIp": "192.168.1.1",
  "method": "POST",
  "endpoint": "/api/v1/currency/convert",
  "responseCode": 200,
  "responseTime": 145.2
}
```

### Key Metrics Logged
- ✅ Client IP address
- ✅ ClientId from JWT token (when implemented)
- ✅ HTTP Method & Target Endpoint
- ✅ Response Code & Response Time
- ✅ Frankfurter API correlation for debugging

## 🔒 Security Features

### Current Implementation
- **Input Validation**: Comprehensive validation
- **Security Headers**: XSS, CSRF, content-type protection
- **HTTPS Enforcement**: Automatic redirection
- **Error Handling**: Secure error responses

### Ready for Enhancement
- **JWT Authentication**: Framework in place
- **Rate Limiting**: Infrastructure ready
- **API Keys**: Extensible authentication

## 📈 Performance Optimizations

- **Async/Await**: Full async implementation
- **Memory Caching**: Configurable in-memory cache
- **Connection Pooling**: HTTP client connection reuse
- **Response Compression**: Available when needed
- **Pagination**: Efficient data handling

## 🔄 Integration with Frankfurter API

### Base URL
`https://api.frankfurter.app/`

### Example Integrations
```bash
# Latest rates
GET https://api.frankfurter.app/latest?from=EUR&to=USD,GBP

# Historical rates  
GET https://api.frankfurter.app/2020-01-01..2020-01-31?from=EUR

# Single conversion
GET https://api.frankfurter.app/latest?from=USD&to=EUR
```

## 🎯 Future Enhancements

### Phase 2 Features
1. **JWT Authentication** - Complete RBAC implementation
2. **Multiple Providers** - Add backup exchange rate providers
3. **Redis Caching** - Distributed caching for scaling
4. **OpenTelemetry** - Distributed tracing implementation
5. **Health Checks** - Advanced health monitoring
6. **Docker Support** - Containerization
7. **CI/CD Pipeline** - Automated deployment

### Provider Extensibility
```csharp
// Ready for multiple providers
services.AddScoped<IExchangeRateProvider, FrankfurterApiProvider>();
services.AddScoped<IExchangeRateProvider, AlternativeProvider>();
// Factory pattern ready for provider selection
```

## 🤝 Contributing

1. Follow the existing architecture patterns
2. Ensure excluded currencies (TRY, PLN, THB, MXN) remain blocked
3. Add comprehensive tests (aim for 90%+ coverage)
4. Update documentation for new features
5. Follow structured logging patterns

## ⚠️ Important Notes

- **Excluded Currencies**: TRY, PLN, THB, MXN are **permanently excluded** per requirements
- **Frankfurter Dependency**: Primary integration with Frankfurter API
- **Caching Strategy**: 15-minute cache duration for optimal performance vs. data freshness
- **Error Handling**: Graceful degradation and comprehensive error responses

---

**API Status**: ✅ **Running on https://localhost:7001 and http://localhost:5001**  
**Swagger Documentation**: Available at root URL  
**Health Check**: `GET /health`
