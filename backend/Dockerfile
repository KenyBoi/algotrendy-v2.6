# ============================================================================
# AlgoTrendy v2.6 API - Multi-Stage Dockerfile
# ============================================================================
# Stage 1: Build Stage
# Stage 2: Runtime Stage
# Optimized for production with security best practices
# ============================================================================

# ============================================================================
# Stage 1: Build Stage
# ============================================================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS builder

# Set working directory
WORKDIR /src

# Copy solution file
COPY AlgoTrendy.sln ./

# Copy all project files (for restore)
COPY AlgoTrendy.API/AlgoTrendy.API.csproj ./AlgoTrendy.API/
COPY AlgoTrendy.Core/AlgoTrendy.Core.csproj ./AlgoTrendy.Core/
COPY AlgoTrendy.Infrastructure/AlgoTrendy.Infrastructure.csproj ./AlgoTrendy.Infrastructure/
COPY AlgoTrendy.TradingEngine/AlgoTrendy.TradingEngine.csproj ./AlgoTrendy.TradingEngine/
COPY AlgoTrendy.DataChannels/AlgoTrendy.DataChannels.csproj ./AlgoTrendy.DataChannels/

# Restore dependencies (cached layer)
RUN dotnet restore AlgoTrendy.API/AlgoTrendy.API.csproj

# Copy all source code
COPY . .

# Build and publish the API project
WORKDIR /src/AlgoTrendy.API
RUN dotnet publish AlgoTrendy.API.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    --verbosity minimal

# ============================================================================
# Stage 2: Runtime Stage
# ============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

# Install curl for health checks
RUN apt-get update && \
    apt-get install -y --no-install-recommends curl && \
    rm -rf /var/lib/apt/lists/*

# Create non-root user for security
RUN groupadd -r algotrendy && \
    useradd -r -g algotrendy -u 1001 algotrendy

# Set working directory
WORKDIR /app

# Copy published application from builder stage
COPY --from=builder /app/publish .

# Create logs directory and set permissions
RUN mkdir -p /app/logs && \
    chown -R algotrendy:algotrendy /app

# Switch to non-root user
USER algotrendy

# Expose ports
# 5002: HTTP API
EXPOSE 5002

# Environment variables (can be overridden)
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:5002 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_EnableDiagnostics=0

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:5002/health || exit 1

# Set entrypoint
ENTRYPOINT ["dotnet", "AlgoTrendy.API.dll"]
