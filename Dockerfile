###############
# Multi-stage build for CurrencyConversionApi (.NET 9)
###############

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and restore
COPY CurrencyConversion.sln ./
COPY CurrencyConversionApi/*.csproj CurrencyConversionApi/
COPY CurrencyConversionApi.Tests/*.csproj CurrencyConversionApi.Tests/
RUN dotnet restore "CurrencyConversionApi/CurrencyConversionApi.csproj"

# Copy all sources
COPY CurrencyConversionApi/. CurrencyConversionApi/

RUN dotnet publish CurrencyConversionApi/CurrencyConversionApi.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS final
WORKDIR /app

# Non-root user
RUN addgroup -S app && adduser -S app -G app
USER app

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production

# (Optional) Set configuration via environment variables for ECS
# Example secrets/vars (define in ECS task definition / Parameter Store):
#   Jwt__SecretKey=CHANGE_IN_PROD
#   Jwt__Issuer=CurrencyConversionApi
#   Jwt__Audience=CurrencyConversionApi
#   ExchangeRateConfig__MaxRetryAttempts=3
#   SmartCache__BusinessHoursTTL=00:15:00

EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=3s --start-period=20s --retries=3 \
  CMD wget -qO- http://127.0.0.1:8080/health || exit 1

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "CurrencyConversionApi.dll"]
