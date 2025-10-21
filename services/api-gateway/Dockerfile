# API Gateway - Main entry point for all microservices
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5000
EXPOSE 5001

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy all project files
COPY ["backend/AlgoTrendy.Core/AlgoTrendy.Core.csproj", "AlgoTrendy.Core/"]
COPY ["backend/AlgoTrendy.Infrastructure/AlgoTrendy.Infrastructure.csproj", "AlgoTrendy.Infrastructure/"]
COPY ["backend/AlgoTrendy.TradingEngine/AlgoTrendy.TradingEngine.csproj", "AlgoTrendy.TradingEngine/"]
COPY ["backend/AlgoTrendy.DataChannels/AlgoTrendy.DataChannels.csproj", "AlgoTrendy.DataChannels/"]
COPY ["backend/AlgoTrendy.Backtesting/AlgoTrendy.Backtesting.csproj", "AlgoTrendy.Backtesting/"]
COPY ["backend/AlgoTrendy.API/AlgoTrendy.API.csproj", "AlgoTrendy.API/"]

# Restore dependencies
RUN dotnet restore "AlgoTrendy.API/AlgoTrendy.API.csproj"

# Copy source code
COPY backend/AlgoTrendy.Core/ ./AlgoTrendy.Core/
COPY backend/AlgoTrendy.Infrastructure/ ./AlgoTrendy.Infrastructure/
COPY backend/AlgoTrendy.TradingEngine/ ./AlgoTrendy.TradingEngine/
COPY backend/AlgoTrendy.DataChannels/ ./AlgoTrendy.DataChannels/
COPY backend/AlgoTrendy.Backtesting/ ./AlgoTrendy.Backtesting/
COPY backend/AlgoTrendy.API/ ./AlgoTrendy.API/

# Build
WORKDIR "/src/AlgoTrendy.API"
RUN dotnet build "AlgoTrendy.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AlgoTrendy.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:5000/health || exit 1

USER $APP_UID

ENTRYPOINT ["dotnet", "AlgoTrendy.API.dll"]
