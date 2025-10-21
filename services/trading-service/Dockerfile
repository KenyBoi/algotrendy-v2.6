# Trading Service - Microservice for order execution and broker management
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5001

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy shared core and dependencies
COPY ["backend/AlgoTrendy.Core/AlgoTrendy.Core.csproj", "AlgoTrendy.Core/"]
COPY ["backend/AlgoTrendy.TradingEngine/AlgoTrendy.TradingEngine.csproj", "AlgoTrendy.TradingEngine/"]

# Restore dependencies
RUN dotnet restore "AlgoTrendy.TradingEngine/AlgoTrendy.TradingEngine.csproj"

# Copy source code
COPY backend/AlgoTrendy.Core/ ./AlgoTrendy.Core/
COPY backend/AlgoTrendy.TradingEngine/ ./AlgoTrendy.TradingEngine/

# Build
WORKDIR "/src/AlgoTrendy.TradingEngine"
RUN dotnet build "AlgoTrendy.TradingEngine.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AlgoTrendy.TradingEngine.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Health check endpoint (to be implemented)
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:5001/health || exit 1

# Run as non-root user
USER $APP_UID

ENTRYPOINT ["dotnet", "AlgoTrendy.TradingEngine.dll"]
