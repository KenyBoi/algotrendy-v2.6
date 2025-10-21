# AlgoTrendy Microservices Architecture (v3.0)

This directory contains the microservices architecture for AlgoTrendy v3.0.

## Architecture Overview

The modular branch restructures AlgoTrendy into independent microservices while keeping the same core codebase from the main branch. This allows:

- **Independent scaling** - Scale services based on load
- **Technology flexibility** - Use different tech stacks per service
- **Fault isolation** - Service failures don't bring down the entire system
- **Independent deployment** - Deploy services separately

## Services

### 1. API Gateway (Port 5000)
**Purpose:** Main entry point for all client requests

**Responsibilities:**
- Request routing to appropriate services
- Authentication and authorization
- Rate limiting
- API documentation (Swagger)

**Code Location:** `backend/AlgoTrendy.API`

---

### 2. Trading Service (Port 5001)
**Purpose:** Order execution and broker management

**Responsibilities:**
- Execute trades across multiple brokers
- Manage broker connections
- Order lifecycle management
- Risk management

**Code Location:** `backend/AlgoTrendy.TradingEngine`

**Supported Brokers:**
- Binance
- Bybit
- Coinbase
- TradeStation
- NinjaTrader
- Interactive Brokers

---

### 3. Data Service (Port 5002)
**Purpose:** Market data aggregation and distribution

**Responsibilities:**
- Real-time market data streaming
- Multi-provider data aggregation
- WebSocket broadcasting (SignalR)
- Historical data management

**Code Location:** `backend/AlgoTrendy.DataChannels`

**Data Providers:**
- Alpaca
- CoinGecko
- EODHD
- Polygon
- Tiingo
- TwelveData

---

### 4. Backtesting Service (Port 5003)
**Purpose:** Strategy backtesting and optimization

**Responsibilities:**
- Historical strategy testing
- Performance metrics calculation
- Multi-engine support (QuantConnect, BacktestingPy, Custom)
- Optimization algorithms

**Code Location:** `backend/AlgoTrendy.Backtesting`

---

## Deployment

### Development (Microservices)
```bash
# Build and start all services
docker-compose -f docker-compose.modular.yml up --build

# Start specific service
docker-compose -f docker-compose.modular.yml up api-gateway

# View logs
docker-compose -f docker-compose.modular.yml logs -f trading-service
```

### Production (Monolith - main branch)
```bash
# Use standard docker-compose for monolith
docker-compose up -d
```

## Service Communication

Services communicate via:
- **HTTP/REST** - Synchronous requests
- **Redis Pub/Sub** - Asynchronous events
- **Shared Database** - QuestDB for time-series data

### Example: Placing an Order

```
Client → API Gateway → Trading Service → Broker
                    ↓
                Redis (Pub/Sub)
                    ↓
            Data Service (Subscribe)
```

## Shared Components

All services share:
- **AlgoTrendy.Core** - Domain models, interfaces, enums
- **QuestDB** - Time-series database
- **Redis** - Caching and pub/sub

## Key Differences from Monolith (main branch)

| Aspect | Monolith (main) | Microservices (modular) |
|--------|----------------|------------------------|
| **Deployment** | Single container | Multiple containers |
| **Scaling** | Scale entire app | Scale per service |
| **Code** | **IDENTICAL** | **IDENTICAL** |
| **Structure** | backend/ directory | services/ directory |
| **Database** | Shared QuestDB | Shared QuestDB (for now) |
| **Complexity** | Low | Medium |

## Migration Strategy

1. **Phase 1** - Both architectures run in parallel
2. **Phase 2** - Beta test modular with subset of users
3. **Phase 3** - Performance comparison and optimization
4. **Phase 4** - Full migration or keep both

## Auto-Sync

Changes pushed to `main` branch automatically sync to `modular` branch via GitHub Actions.

See: `.github/workflows/sync-to-modular.yml`

## Version

- **Main Branch (v2.6.x)** - Production monolith
- **Modular Branch (v3.0.0-beta.x)** - Experimental microservices

## Development Notes

- **IMPORTANT:** Code changes should be made in `backend/` directory
- Dockerfiles reference the `backend/` source code
- Services are wrappers around existing modules
- Same .csproj files, different deployment strategy
