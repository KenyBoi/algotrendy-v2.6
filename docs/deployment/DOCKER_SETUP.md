# AlgoTrendy v2.6 - Docker Setup Guide

Complete guide for running AlgoTrendy using Docker Compose.

## Quick Start (One Command)

```bash
# Clone and start everything
git clone https://github.com/KenyBoi/algotrendy-v2.6.git
cd algotrendy-v2.6
cp .env.example .env  # Copy and configure your credentials
docker-compose up -d
```

That's it! AlgoTrendy is now running at:
- **Frontend**: http://localhost:3000
- **API**: http://localhost:5002
- **Swagger Docs**: http://localhost:5002/swagger
- **QuestDB Console**: http://localhost:9000
- **Seq Logs**: http://localhost:5341

---

## Table of Contents

- [Prerequisites](#prerequisites)
- [Services Overview](#services-overview)
- [Initial Setup](#initial-setup)
- [Running the Application](#running-the-application)
- [Environment Configuration](#environment-configuration)
- [Accessing Services](#accessing-services)
- [Managing Containers](#managing-containers)
- [Troubleshooting](#troubleshooting)
- [Advanced Configuration](#advanced-configuration)

---

## Prerequisites

### Required
- **Docker** 24.0+ ([Install Docker](https://docs.docker.com/get-docker/))
- **Docker Compose** 2.0+ (included with Docker Desktop)
- **Git** (to clone the repository)

### Recommended
- **4GB RAM minimum** (8GB+ recommended)
- **10GB free disk space**
- **Internet connection** (for pulling images and market data)

### Verify Installation

```bash
docker --version
# Docker version 24.0.0 or higher

docker-compose --version
# Docker Compose version v2.0.0 or higher
```

---

## Services Overview

AlgoTrendy runs 7 microservices in Docker:

| Service | Purpose | Port | Required |
|---------|---------|------|----------|
| **nginx** | Reverse proxy, SSL termination | 80, 443 | ✅ Yes |
| **frontend** | React trading dashboard | 3000 | ✅ Yes |
| **api** | .NET 8 REST API | 5002 | ✅ Yes |
| **questdb** | Time-series database | 9000, 8812 | ✅ Yes |
| **seq** | Structured logging | 5341 | ✅ Yes |
| **ml-service** | ML prediction service | 5003 | ⚠️ Optional |
| **backtesting-py** | Python backtesting engine | 5004 | ⚠️ Optional |

### Resource Usage

- **Total RAM**: ~2-3GB
- **Total Disk**: ~5GB (images + volumes)
- **CPU**: 2-4 cores recommended

---

## Initial Setup

### Step 1: Clone the Repository

```bash
git clone https://github.com/KenyBoi/algotrendy-v2.6.git
cd algotrendy-v2.6
```

### Step 2: Configure Environment

```bash
# Copy the example environment file
cp .env.example .env

# Edit with your credentials
nano .env  # or use your favorite editor
```

**Minimum required configuration:**

```bash
# QuestDB
QUESTDB_USER=admin
QUESTDB_PASSWORD=your_secure_password

# Binance (use testnet for development)
BINANCE_API_KEY=your_binance_api_key
BINANCE_API_SECRET=your_binance_api_secret
BINANCE_TESTNET=true

# Application
ASPNETCORE_ENVIRONMENT=Development
```

See [Environment Configuration](#environment-configuration) for all options.

### Step 3: Build Images (First Time Only)

```bash
# Build all Docker images
docker-compose build

# Or build specific service
docker-compose build api
```

**Build time**: 5-10 minutes (first time only)

---

## Running the Application

### Start All Services

```bash
# Start in detached mode (background)
docker-compose up -d

# Or start with logs visible
docker-compose up
```

### Verify Services Are Running

```bash
# Check status
docker-compose ps

# Should show 7 services running:
# NAME                    STATUS    PORTS
# algotrendy-nginx        Up        0.0.0.0:80->80/tcp, 443/tcp
# algotrendy-frontend     Up        0.0.0.0:3000->80/tcp
# algotrendy-api          Up        0.0.0.0:5002->5002/tcp
# algotrendy-questdb      Up        0.0.0.0:8812->8812/tcp, 9000/tcp
# algotrendy-seq          Up        0.0.0.0:5341->80/tcp
# algotrendy-ml-service   Up        0.0.0.0:5003->5003/tcp
# algotrendy-backtesting-py Up      0.0.0.0:5004->5004/tcp
```

### Check Health

```bash
# View logs for all services
docker-compose logs

# View logs for specific service
docker-compose logs api
docker-compose logs -f frontend  # Follow logs

# Check health of all containers
docker-compose ps --format "table {{.Name}}\t{{.Status}}"
```

### Wait for Startup

Services take 30-60 seconds to fully start. Wait for:
- ✅ All health checks passing
- ✅ API responding at http://localhost:5002/health
- ✅ Frontend loading at http://localhost:3000

```bash
# Test API health endpoint
curl http://localhost:5002/health

# Should return: {"status":"healthy"}
```

---

## Environment Configuration

### Full .env Configuration

```bash
# ============================================================================
# AlgoTrendy v2.6 - Environment Configuration
# ============================================================================

# ----------------------------------------------------------------------------
# Application Settings
# ----------------------------------------------------------------------------
ASPNETCORE_ENVIRONMENT=Development  # Development | Staging | Production
API_LOG_LEVEL=Information           # Verbose | Debug | Information | Warning | Error
ALLOWED_ORIGINS=http://localhost:3000,https://localhost

# ----------------------------------------------------------------------------
# Database - QuestDB
# ----------------------------------------------------------------------------
QUESTDB_USER=admin
QUESTDB_PASSWORD=quest
# Connection string is auto-generated from above

# ----------------------------------------------------------------------------
# Logging - Seq
# ----------------------------------------------------------------------------
SEQ_API_KEY=                        # Optional: Leave empty for no auth

# ----------------------------------------------------------------------------
# Broker Credentials - Binance
# ----------------------------------------------------------------------------
BINANCE_API_KEY=
BINANCE_API_SECRET=
BINANCE_TESTNET=true                # true = testnet, false = mainnet (CAREFUL!)

# ----------------------------------------------------------------------------
# Broker Credentials - Bybit
# ----------------------------------------------------------------------------
BYBIT_API_KEY=
BYBIT_API_SECRET=
BYBIT_TESTNET=true

# ----------------------------------------------------------------------------
# Broker Credentials - OKX
# ----------------------------------------------------------------------------
OKX_API_KEY=
OKX_API_SECRET=
OKX_PASSPHRASE=
OKX_TESTNET=true

# ----------------------------------------------------------------------------
# Market Data Settings
# ----------------------------------------------------------------------------
MARKET_DATA_FETCH_INTERVAL=60       # Seconds between data fetches

# ----------------------------------------------------------------------------
# ML Service Settings
# ----------------------------------------------------------------------------
ML_ENHANCEMENT_ENABLED=true         # Enable/disable ML predictions
FLASK_ENV=production                # development | production

# ----------------------------------------------------------------------------
# QuantConnect Integration (Optional)
# ----------------------------------------------------------------------------
QUANTCONNECT_USER_ID=
QUANTCONNECT_API_TOKEN=

# ----------------------------------------------------------------------------
# Security (Production Only)
# ----------------------------------------------------------------------------
# JWT_SECRET=                       # Auto-generated if not set
# ENCRYPTION_KEY=                   # Auto-generated if not set
```

### Environment-Specific Configurations

**Development (.env.development)**
```bash
ASPNETCORE_ENVIRONMENT=Development
API_LOG_LEVEL=Debug
BINANCE_TESTNET=true
ML_ENHANCEMENT_ENABLED=true
```

**Production (.env.production)**
```bash
ASPNETCORE_ENVIRONMENT=Production
API_LOG_LEVEL=Warning
BINANCE_TESTNET=false  # ⚠️ LIVE TRADING
ML_ENHANCEMENT_ENABLED=true
```

---

## Accessing Services

### Web Interfaces

| Service | URL | Credentials |
|---------|-----|-------------|
| **Frontend** | http://localhost:3000 | N/A |
| **API Swagger** | http://localhost:5002/swagger | N/A |
| **API Health** | http://localhost:5002/health | N/A |
| **QuestDB Console** | http://localhost:9000 | admin / quest |
| **Seq Logs** | http://localhost:5341 | N/A |
| **Nginx Status** | http://localhost/health | N/A |

### API Endpoints

Base URL: `http://localhost:5002/api`

**Health Check**
```bash
curl http://localhost:5002/health
```

**Market Data**
```bash
curl http://localhost:5002/api/marketdata/BTCUSDT
```

**Place Order** (requires authentication)
```bash
curl -X POST http://localhost:5002/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "symbol": "BTCUSDT",
    "side": "Buy",
    "type": "Market",
    "quantity": 0.001
  }'
```

### Database Access

**QuestDB Web Console**: http://localhost:9000

**SQL Query** (PostgreSQL wire protocol):
```bash
psql -h localhost -p 8812 -U admin -d qdb

# Password: quest (or your QUESTDB_PASSWORD)
```

**Python Connection**:
```python
import psycopg2

conn = psycopg2.connect(
    host='localhost',
    port=8812,
    database='qdb',
    user='admin',
    password='quest'
)

cursor = conn.cursor()
cursor.execute("SELECT * FROM market_data_1m LIMIT 10")
print(cursor.fetchall())
```

---

## Managing Containers

### Start/Stop Services

```bash
# Start all services
docker-compose up -d

# Start specific service
docker-compose up -d api

# Stop all services
docker-compose stop

# Stop specific service
docker-compose stop api

# Restart all services
docker-compose restart

# Restart specific service
docker-compose restart api
```

### View Logs

```bash
# All services
docker-compose logs

# Specific service
docker-compose logs api

# Follow logs (real-time)
docker-compose logs -f api

# Last 100 lines
docker-compose logs --tail=100 api

# Logs since timestamp
docker-compose logs --since 2024-10-21T12:00:00 api
```

### Execute Commands in Containers

```bash
# Open bash shell in API container
docker-compose exec api bash

# Run .NET command
docker-compose exec api dotnet --info

# Check database connection
docker-compose exec api curl http://questdb:9000

# View API logs
docker-compose exec api cat /app/logs/algotrendy-*.log
```

### Clean Up

```bash
# Stop and remove containers (keeps volumes/data)
docker-compose down

# Remove containers AND volumes (⚠️ deletes all data)
docker-compose down -v

# Remove containers, volumes, and images
docker-compose down -v --rmi all

# Remove unused Docker resources
docker system prune -a
```

---

## Troubleshooting

### Service Won't Start

**Check logs:**
```bash
docker-compose logs service-name
```

**Common issues:**

1. **Port already in use**
   ```bash
   # Find process using port
   sudo lsof -i :5002

   # Kill process
   sudo kill -9 <PID>

   # Or change port in docker-compose.yml
   ports:
     - "5003:5002"  # Host:Container
   ```

2. **Image build failure**
   ```bash
   # Rebuild without cache
   docker-compose build --no-cache api
   ```

3. **Volume permission issues**
   ```bash
   # Fix permissions
   sudo chown -R $USER:$USER volumes/
   ```

### Container Keeps Restarting

```bash
# Check health status
docker-compose ps

# View detailed logs
docker-compose logs --tail=100 service-name

# Check resource usage
docker stats

# Inspect container
docker inspect algotrendy-api
```

### Database Connection Failed

```bash
# Verify QuestDB is running
docker-compose ps questdb

# Test connection from API container
docker-compose exec api curl http://questdb:9000

# Check QuestDB logs
docker-compose logs questdb

# Verify credentials in .env
cat .env | grep QUESTDB
```

### API Returns 500 Errors

```bash
# Check API logs
docker-compose logs api

# Check Seq logs (more detailed)
open http://localhost:5341

# Verify environment variables
docker-compose exec api env | grep -i binance

# Restart API
docker-compose restart api
```

### Frontend Not Loading

```bash
# Check if frontend is running
docker-compose ps frontend

# View frontend logs
docker-compose logs frontend

# Check API connectivity from frontend
docker-compose exec frontend curl http://api:5002/health

# Rebuild frontend
docker-compose build --no-cache frontend
docker-compose up -d frontend
```

---

## Advanced Configuration

### Running Specific Services

```bash
# Only run database and API (no frontend)
docker-compose up -d questdb seq api

# Run without ML services
docker-compose up -d nginx frontend api questdb seq
```

### Development Mode with Hot Reload

```bash
# Mount source code for live updates
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up
```

### Production Deployment

```bash
# Use production environment
cp .env.production .env

# Build for production
docker-compose build --no-cache

# Start with production config
docker-compose up -d

# Enable SSL
./scripts/setup-ssl.sh
```

### Resource Limits

Edit `docker-compose.yml` to add resource limits:

```yaml
api:
  # ... existing config ...
  deploy:
    resources:
      limits:
        cpus: '2.0'
        memory: 2G
      reservations:
        cpus: '1.0'
        memory: 1G
```

### Custom Network

```yaml
networks:
  algotrendy-network:
    driver: bridge
    ipam:
      config:
        - subnet: 172.25.0.0/16  # Change subnet
```

---

## Maintenance

### Backup Data

```bash
# Backup QuestDB data
docker run --rm \
  -v algotrendy_questdb_data:/data \
  -v $(pwd)/backups:/backup \
  alpine tar czf /backup/questdb-$(date +%Y%m%d).tar.gz /data

# Backup logs
docker run --rm \
  -v algotrendy_api_logs:/logs \
  -v $(pwd)/backups:/backup \
  alpine tar czf /backup/logs-$(date +%Y%m%d).tar.gz /logs
```

### Restore Data

```bash
# Restore QuestDB data
docker run --rm \
  -v algotrendy_questdb_data:/data \
  -v $(pwd)/backups:/backup \
  alpine tar xzf /backup/questdb-20241021.tar.gz -C /
```

### Update Images

```bash
# Pull latest images
docker-compose pull

# Rebuild custom images
docker-compose build --no-cache

# Restart with new images
docker-compose up -d
```

### Monitor Resources

```bash
# Real-time stats
docker stats

# Disk usage
docker system df

# Volume sizes
docker volume ls -q | xargs docker volume inspect | grep -A1 Mountpoint
```

---

## Next Steps

After getting Docker running:

1. **Configure Credentials** - Add your broker API keys to `.env`
2. **Explore the API** - Visit http://localhost:5002/swagger
3. **Check Logs** - Monitor system at http://localhost:5341
4. **Run Tests** - Execute integration tests in the API container
5. **Deploy to Production** - Follow [DEPLOYMENT_GUIDE.md](docs/DEPLOYMENT_GUIDE.md)

---

## Support

- **Documentation**: [docs/](docs/)
- **Issues**: https://github.com/KenyBoi/algotrendy-v2.6/issues
- **Discussions**: https://github.com/KenyBoi/algotrendy-v2.6/discussions

---

**Last Updated**: October 21, 2025
**Docker Compose Version**: 2.0+
**AlgoTrendy Version**: 2.6.0
