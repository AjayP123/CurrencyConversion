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


## 📊 AWS ECS Deployment Guide

This section provides a complete guide for deploying the Currency Conversion API on AWS ECS Fargate with production-grade infrastructure including Load Balancer, Auto Scaling, WAF, and monitoring.

### 🏗️ Infrastructure Architecture

```
Internet Traffic
      ↓
  AWS WAF (Rate Limiting: 2000 req/5min)
      ↓
Application Load Balancer
      ↓
ECS Fargate Service (Auto Scaling: 2-10 tasks)
      ↓
Docker Containers (Currency API)
      ↓
CloudWatch (Logging & Monitoring)
```

### 🚀 Deployment Features

- **Container Orchestration**: ECS Fargate (serverless containers)
- **Load Balancing**: Application Load Balancer with health checks
- **Auto Scaling**: CPU, Memory, and Request-based scaling
- **Security**: WAF with rate limiting and AWS managed rules
- **Networking**: Custom VPC with public subnets across AZs
- **Monitoring**: CloudWatch logging with 7-day retention
- **Container Registry**: ECR with lifecycle policies

### 📋 Prerequisites

1. **AWS CLI v2** installed and configured
2. **Docker** installed and running
3. **AWS Account** with appropriate permissions
4. **Git** to clone the repository

### ⚠️ Important Deployment Order

**Why this specific order matters:**
- The CloudFormation stack includes an ECS Service that references a Docker image in ECR
- If the image doesn't exist when the stack is created, the ECS tasks will fail to start
- By creating the ECR repository first and pushing the image, we ensure the ECS service can successfully pull and run the container

This approach follows AWS best practices for containerized application deployment.

### 🔧 Step-by-Step Deployment

#### Step 1: Clone and Prepare the Repository

```bash
# Clone the repository
git clone https://github.com/AjayP123/CurrencyConversion.git
cd CurrencyConversion

# Ensure you have the cloudformation-template.yaml file
ls cloudformation-template.yaml
```

#### Step 2: Create ECR Repository (Infrastructure Only)

First, we need to create just the ECR repository so we can push our Docker image:

```bash
# Create a minimal stack with just ECR repository
aws ecr create-repository \
  --repository-name currency-conversion-api-v2 \
  --region eu-west-1
```

#### Step 3: Build and Push Docker Image

```bash
# Get ECR login token and login
aws ecr get-login-password --region eu-west-1 | \
  docker login --username AWS --password-stdin \
  $(aws sts get-caller-identity --query Account --output text).dkr.ecr.eu-west-1.amazonaws.com

# Build the Docker image
docker build -t currency-conversion-api-v2 .

# Tag the image for ECR
docker tag currency-conversion-api-v2:latest \
  $(aws sts get-caller-identity --query Account --output text).dkr.ecr.eu-west-1.amazonaws.com/currency-conversion-api-v2:latest

# Push the image to ECR
docker push $(aws sts get-caller-identity --query Account --output text).dkr.ecr.eu-west-1.amazonaws.com/currency-conversion-api-v2:latest
```

#### Step 4: Deploy Complete AWS Infrastructure

Now that the Docker image exists in ECR, we can deploy the full infrastructure:

```bash
# Deploy the CloudFormation stack with all resources
aws cloudformation create-stack \
  --stack-name currency-conversion-api-v2 \
  --template-body file://cloudformation-template.yaml \
  --capabilities CAPABILITY_IAM \
  --region eu-west-1

# Monitor stack creation progress
aws cloudformation describe-stacks \
  --stack-name currency-conversion-api-v2 \
  --region eu-west-1 \
  --query 'Stacks[0].StackStatus'

# Wait for stack creation to complete (this may take 5-10 minutes)
aws cloudformation wait stack-create-complete \
  --stack-name currency-conversion-api-v2 \
  --region eu-west-1
```

#### Step 5: Verify Deployment

```bash
# Get the Load Balancer URL
aws cloudformation describe-stacks \
  --stack-name currency-conversion-api-v2 \
  --region eu-west-1 \
  --query 'Stacks[0].Outputs[?OutputKey==`LoadBalancerURL`].OutputValue' \
  --output text

# Test the health endpoint
curl http://your-load-balancer-url/health

# Access Swagger UI
curl http://your-load-balancer-url/swagger
```

### 🔧 Infrastructure Components

#### 1. **Networking**
```yaml
VPC: 10.0.0.0/16
Public Subnets: 
  - 10.0.1.0/24 (eu-west-1a)
  - 10.0.2.0/24 (eu-west-1b)
Security Groups:
  - ALB: Ports 80/443 from Internet
  - ECS: Port 8080 from ALB only
```

#### 2. **ECS Configuration**
```yaml
Cluster: currency-conversion-api-v2-cluster
Service: currency-conversion-api-v2-service
Task Definition:
  CPU: 256 units
  Memory: 512 MB
  Platform: Fargate
```

#### 3. **Auto Scaling Policies**
```yaml
CPU Utilization: 70% target
Memory Utilization: 80% target
Request Count: 1000 requests/minute per target
Min Capacity: 2 tasks
Max Capacity: 10 tasks
```

#### 4. **Load Balancer**
```yaml
Health Check:
  Path: /health
  Interval: 30 seconds
  Timeout: 10 seconds
  Healthy Threshold: 2
  Unhealthy Threshold: 5
```

#### 5. **WAF Rules**
```yaml
Rate Limiting: 2000 requests per 5 minutes per IP
AWS Managed Rules: Common Rule Set for OWASP protection
```

### 📊 Monitoring & Observability

#### CloudWatch Metrics
- **ECS Service**: CPU/Memory utilization, task count
- **Load Balancer**: Request count, latency, error rates
- **Auto Scaling**: Scaling activities and alarms

#### Log Groups
```bash
# View application logs
aws logs tail /ecs/currency-conversion-api-v2 --follow --region eu-west-1
```

