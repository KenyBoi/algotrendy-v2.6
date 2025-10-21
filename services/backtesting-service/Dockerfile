# Backtesting Service - Microservice for strategy backtesting and optimization
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5003

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy shared core and dependencies
COPY ["backend/AlgoTrendy.Core/AlgoTrendy.Core.csproj", "AlgoTrendy.Core/"]
COPY ["backend/AlgoTrendy.Backtesting/AlgoTrendy.Backtesting.csproj", "AlgoTrendy.Backtesting/"]

# Restore dependencies
RUN dotnet restore "AlgoTrendy.Backtesting/AlgoTrendy.Backtesting.csproj"

# Copy source code
COPY backend/AlgoTrendy.Core/ ./AlgoTrendy.Core/
COPY backend/AlgoTrendy.Backtesting/ ./AlgoTrendy.Backtesting/

# Build
WORKDIR "/src/AlgoTrendy.Backtesting"
RUN dotnet build "AlgoTrendy.Backtesting.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AlgoTrendy.Backtesting.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:5003/health || exit 1

USER $APP_UID

ENTRYPOINT ["dotnet", "AlgoTrendy.Backtesting.dll"]
