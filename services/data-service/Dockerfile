# Data Service - Microservice for market data providers and channels
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5002

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy shared core and dependencies
COPY ["backend/AlgoTrendy.Core/AlgoTrendy.Core.csproj", "AlgoTrendy.Core/"]
COPY ["backend/AlgoTrendy.Infrastructure/AlgoTrendy.Infrastructure.csproj", "AlgoTrendy.Infrastructure/"]
COPY ["backend/AlgoTrendy.DataChannels/AlgoTrendy.DataChannels.csproj", "AlgoTrendy.DataChannels/"]

# Restore dependencies
RUN dotnet restore "AlgoTrendy.DataChannels/AlgoTrendy.DataChannels.csproj"

# Copy source code
COPY backend/AlgoTrendy.Core/ ./AlgoTrendy.Core/
COPY backend/AlgoTrendy.Infrastructure/ ./AlgoTrendy.Infrastructure/
COPY backend/AlgoTrendy.DataChannels/ ./AlgoTrendy.DataChannels/

# Build
WORKDIR "/src/AlgoTrendy.DataChannels"
RUN dotnet build "AlgoTrendy.DataChannels.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AlgoTrendy.DataChannels.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:5002/health || exit 1

USER $APP_UID

ENTRYPOINT ["dotnet", "AlgoTrendy.DataChannels.dll"]
