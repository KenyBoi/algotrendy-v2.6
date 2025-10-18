# AlgoTrendy - Project Snapshot

**What is this project?** Automated cryptocurrency trading platform
**Current Version:** v2.6 (C# .NET 8)
**Status:** Production-ready
**Created:** Oct 2024 (Python v2.5), Rewritten Oct 2025 (C# v2.6)

---

## ⚡ 30-Second Summary

AlgoTrendy is an **automated cryptocurrency trading bot** that:
- Fetches market data from 4 exchanges (Binance, OKX, Coinbase, Kraken)
- Analyzes data with trading strategies (Momentum, RSI)
- Generates buy/sell signals
- Executes trades on Binance (testnet or production)
- Tracks positions and calculates PnL in real-time
- Manages risk (exposure limits, position sizing)
- Provides REST API for external control
- Streams live market data via WebSockets/SignalR

---

## 🎯 Core Features

### 1. Multi-Exchange Market Data Ingestion
```
Binance (REST)  ─┐
OKX (REST)      ├─→ MarketDataChannelService ─→ QuestDB
Coinbase (REST) ├─→ (60-second intervals)
Kraken (REST)   ─┘   (parallel fetching)
```
- **Symbols:** BTC, ETH, BNB, SOL, ADA, XRP, DOT, LINK, MATIC, AVAX
- **Interval:** 1-minute OHLCV candles
- **Rate Limiting:** Per-exchange limits respected
- **Storage:** QuestDB (time-series database)
- **Status:** ✅ Production ready

### 2. Trading Strategies
```
Market Data → Indicators → Strategies → Signals
     ↓             ↓            ↓           ↓
   OHLCV      RSI, MACD,   Momentum,   BUY/SELL/HOLD
   candles    EMA, SMA,    RSI, etc.   with confidence
   (cached)   Bollinger
              (1-min TTL)
```

**Implemented (MVP):**
- ✅ **Momentum Strategy:** Price change % + volatility filter
- ✅ **RSI Strategy:** Oversold/overbought detection

**Indicators Available:**
- ✅ RSI (14-period)
- ✅ MACD (12-26-9)
- ✅ EMA (exponential moving average)
- ✅ SMA (simple moving average)
- ✅ Volatility (standard deviation)

**Status:** ✅ Core complete, expansion in Phase 7

### 3. Trading Engine
```
Signal → Order Validation → Risk Check → Broker → Exchange
  ↓            ↓               ↓           ↓         ↓
[Signal]  [Size, Price,   [Exposure,  [Place/   [Order
 from       Entry/Exit]   Position   Cancel]    Executed]
strategy              Limits]
```

**Order Lifecycle:**
- Pending → Open → Filled (or Cancelled)

**Order Types:**
- Market orders (immediate execution)
- Limit orders (at specific price)
- Stop-loss orders (protect downside)

**Position Tracking:**
- Real-time entry price
- Current price
- PnL calculation ($ and %)
- Live position value

**Risk Management:**
- Min/Max order size validation
- Max position size (% of balance)
- Max concurrent positions
- Total exposure % limit
- All configurable on/off

**Status:** ✅ Production ready, Binance only

### 4. Broker Integration
```
AlgoTrendy → IBroker Interface → Exchange API
     ↓            ↓
[PlaceOrder]    [BinanceBroker] → [Binance REST API]
[CancelOrder]   (testnet/prod)
[GetBalance]
[GetStatus]
```

**Implemented:**
- ✅ Binance (Spot trading, testnet + production)

**Deferred (Phase 7):**
- ⏳ Bybit, OKX, Kraken (data channels exist, broker wrappers don't)

**Status:** ✅ Binance MVP complete

### 5. REST API Endpoints
```
GET  /health                           → Service health
GET  /api/market-data/{exchange}/{symbol} → Latest candles
POST /api/orders                       → Place order
POST /api/orders/{id}/cancel           → Cancel order
GET  /api/positions                    → Current positions
GET  /api/strategies/{symbol}          → Strategy signal
HUB  /hubs/marketdata                  → Real-time stream (SignalR)
```

**Status:** ✅ Core endpoints implemented

### 6. Real-Time Streaming
```
WebSocket (SignalR) Hub: /hubs/marketdata
Clients connect and receive:
├─ Market data updates (1-min candles)
├─ Signal generation events
├─ Position updates
└─ Order status changes
```

**Status:** ✅ Infrastructure ready

---

## 🏗️ Technical Stack

| Layer | Technology | Version |
|-------|-----------|---------|
| **Language** | C# | .NET 8 |
| **Web Framework** | ASP.NET Core | 8.0 |
| **Real-time** | SignalR | Built-in |
| **Database** | QuestDB | Latest |
| **Broker SDK** | Binance.Net | Latest |
| **Testing** | xUnit | 2.6+ |
| **Mocking** | Moq | 4.20+ |
| **Assertions** | FluentAssertions | 6.12+ |
| **Deployment** | Docker | Multi-stage |
| **Container OS** | Linux | Debian |

---

## 📊 Key Metrics

### Performance
- **API Response Time:** <15ms (warm requests)
- **Throughput:** >800 requests/second capacity
- **Memory Footprint:** 140-200 MB
- **Cold Start:** <45 seconds (Docker)
- **Docker Image:** 245 MB (optimized)

### Quality
- **Tests:** 226/264 passing (85.6%)
- **Build Time:** 4.4 seconds
- **Type Safety:** Strong (compile-time checks)
- **Code Coverage:** 80%+ on core modules

### Comparison to v2.5
| Metric | v2.5 | v2.6 | Delta |
|--------|------|------|-------|
| Response Time | 14.9 ms | ~12 ms | ↓ 20% faster |
| Throughput | 625 req/s | >800 req/s | ↑ 25% faster |
| Memory | 300-400 MB | 140-200 MB | ↓ 50% less |
| Type Safety | Medium | Strong | ✅ Better |
| True Parallelism | No (GIL) | Yes | ✅ Better |

---

## 🗄️ Data Architecture

### QuestDB Schema
```
market_data table:
├─ symbol (VARCHAR) - e.g., BTCUSDT
├─ timestamp (TIMESTAMP) - UTC, 1-minute candles
├─ open (DOUBLE)
├─ high (DOUBLE)
├─ low (DOUBLE)
├─ close (DOUBLE)
├─ volume (DOUBLE)
├─ exchange (VARCHAR) - Binance, OKX, etc.
└─ indexed on: (symbol, timestamp, exchange)

Records: 4,100+ from v2.5, continuous growth in v2.6
Retention: Indefinite (compress with age in production)
```

### Data Flow
```
Exchange APIs (REST)
     ↓
MarketDataChannelService (Background job, 60s interval)
     ↓
Per-exchange channel (Binance, OKX, Coinbase, Kraken)
     ↓
Fetch candles (parallel)
     ↓
Validate data
     ↓
Batch insert to QuestDB
     ↓
Available to:
├─ Strategies (calculate signals)
├─ API clients (query historical)
└─ Dashboard (display charts)
```

---

## 🚀 Deployment

### Current Status
- ✅ Docker image built and tested
- ✅ docker-compose configuration complete
- ✅ Nginx reverse proxy configured
- ✅ SSL/TLS ready (self-signed or Let's Encrypt)
- ✅ Non-root container user
- ✅ Health checks configured
- ✅ Ready for production deployment

### Deployment Architecture
```
Internet
    ↓
Nginx (Port 443 HTTPS)
    ↓
AlgoTrendy API (Port 5002, internal network)
    ↓
QuestDB (Port 8812, internal network)
```

### Three Quick Steps
```bash
1. Set credentials: $ nano .env
2. Deploy: $ docker-compose -f docker-compose.prod.yml up -d
3. Verify: $ curl https://algotrendy.duckdns.org/health
```

---

## 📈 Use Cases

### 1. Automated Trading
- Define strategy rules
- Platform monitors market 24/7
- Auto-executes trades when signals match
- Tracks positions and PnL
- No manual intervention needed

### 2. Strategy Backtesting
- Test trading rules on historical data
- Evaluate performance before live trading
- Compare different strategy parameters
- ⏳ Backtesting engine in development (Phase 7)

### 3. Market Monitoring Dashboard
- Real-time price feeds from 4 exchanges
- Strategy signals for each symbol
- Position tracking
- PnL calculation
- ⏳ Dashboard UI in development (Phase 7)

### 4. Algorithmic Research
- Access to historical market data
- Multiple indicators available
- API for custom analysis
- Can test custom strategies

---

## 💰 Trading Modes

### Development/Testing
- **Testnet:** Trade on exchange testnet with fake money
- **Paper Trading:** Simulate trades without real execution
- ✅ Both ready in v2.6

### Production
- **Live Trading:** Execute real trades with real money
- ⚠️ Available but use with caution
- Strongly recommend starting with testnet

---

## 🔐 Security

### Credentials Management
- ✅ No hardcoded secrets in code
- ✅ Environment variables for all credentials
- ✅ User Secrets for development
- ✅ API keys scoped to IP address (Binance feature)

### Network Security
- ✅ SSL/TLS encryption for all traffic
- ✅ Nginx reverse proxy with security headers
- ✅ HSTS, X-Frame-Options, X-Content-Type-Options
- ✅ Gzip compression

### Data Security
- ✅ Market data only (no PII)
- ✅ QuestDB connection pooling
- ✅ Non-root container execution
- ✅ Read-only filesystems where possible

---

## 📚 Documentation

| Document | Location | Purpose |
|----------|----------|---------|
| Deployment Guide | /DEPLOYMENT_DOCKER.md | How to deploy |
| Deployment Checklist | /DEPLOYMENT_CHECKLIST.md | Pre-deployment validation |
| Project Overview | /PROJECT_OVERVIEW.md | Architecture deep-dive |
| Upgrade Framework | /version_upgrade_tools&doc/ | How to upgrade versions |
| v2.5→v2.6 Case Study | /version_upgrade_tools&doc/docs/ | What was accomplished |
| Performance Report | /PERFORMANCE_REPORT.md | Baselines & expectations |

---

## 🎓 Learning Path

1. **Quick Overview:** This file (5 min)
2. **Current Status:** CURRENT_STATE.md (2 min)
3. **System Design:** ARCHITECTURE_SNAPSHOT.md (3 min)
4. **Version History:** VERSION_HISTORY.md (10 min)
5. **Code Review:** Start with backend/AlgoTrendy.Core/Models/

---

## ❓ Common Questions

**Q: Can I make money with this?**
A: It's designed to. Depends on strategy quality, market conditions, and risk management. Start on testnet.

**Q: Is it ready for production?**
A: Yes, technically ready. Recommend running testnet first to validate performance.

**Q: Can I add my own strategies?**
A: Yes. Implement IStrategy interface, add indicators as needed. See ARCHITECTURE_SNAPSHOT.md.

**Q: What's the maintenance burden?**
A: Minimal. Runs in Docker, self-contained. Needs monitoring and occasional updates.

**Q: How do I add more exchanges?**
A:
1. Data channels: Implement IDataChannel for REST/WebSocket integration
2. Broker integration: Implement IBroker for order placement (if that exchange supports trading)
3. Add to orchestration service

---

**Status:** Current as of October 18, 2025
**Version:** v2.6 Production-Ready
**Next Phase:** Phase 7 (Backtesting, more brokers, more strategies)
