# AlgoTrendy v2.6 - Architecture Diagrams

## System Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         AlgoTrendy v2.6 Architecture                          │
└─────────────────────────────────────────────────────────────────────────────┘

┌──────────────────────┐        ┌──────────────────────┐
│   Frontend Layer     │        │   External Systems   │
│                      │        │                      │
│  ┌───────────────┐  │        │  ┌───────────────┐  │
│  │  React+Vite   │◄─┼────────┼──┤  TradingView  │  │
│  │   Dashboard   │  │        │  │   Webhooks    │  │
│  └──────┬────────┘  │        │  └───────────────┘  │
│         │            │        │                      │
│         │  HTTP/WS   │        │  ┌───────────────┐  │
└─────────┼────────────┘        │  │  Discord/     │  │
          │                     │  │  Slack/       │  │
          ▼                     │  │  Telegram     │  │
┌─────────────────────────────┐│  └───────────────┘  │
│      Nginx (Reverse Proxy)   ││                      │
│   - SSL Termination          ││                      │
│   - Load Balancing           ││                      │
│   - Rate Limiting            ││                      │
└──────────┬──────────────────┘└──────────────────────┘
           │
           ▼
┌───────────────────────────────────────────────────────────┐
│                  AlgoTrendy API (.NET 8)                   │
│                                                            │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐   │
│  │ Controllers  │  │  Middleware  │  │   Services   │   │
│  │ - Trading    │  │ - Auth       │  │ - Webhook    │   │
│  │ - Backtest   │  │ - Metrics    │  │ - Broker     │   │
│  │ - Portfolio  │  │ - CORS       │  │ - ML         │   │
│  │ - Market     │  │ - Logging    │  │ - Backtest   │   │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘   │
│         │                  │                  │            │
│         └──────────────────┴──────────────────┘            │
│                            │                                │
└────────────────────────────┼────────────────────────────────┘
                             │
                 ┌───────────┼───────────┐
                 │           │           │
                 ▼           ▼           ▼
     ┌──────────────┐  ┌─────────┐  ┌──────────┐
     │   QuestDB    │  │   Seq   │  │  Redis   │
     │  (Time-      │  │  (Logs) │  │  (Cache) │
     │   Series)    │  └─────────┘  └──────────┘
     └──────────────┘
            │
            │ Market Data
            │ Orders
            │ Positions
            ▼
   ┌─────────────────────────────────────┐
   │        Data Channels Layer           │
   │                                      │
   │  ┌────────┐  ┌────────┐  ┌────────┐│
   │  │Binance │  │  OKX   │  │Coinbase││
   │  │  REST  │  │  REST  │  │  REST  ││
   │  │   WS   │  │   WS   │  │   WS   ││
   │  └────────┘  └────────┘  └────────┘│
   │                                      │
   │  ┌────────┐  ┌────────┐  ┌────────┐│
   │  │Kraken  │  │  MEXC  │  │ Bybit  ││
   │  │  REST  │  │  REST  │  │  REST  ││
   │  └────────┘  └────────┘  └────────┘│
   └──────────────────────────────────────┘

   ┌─────────────────────────────────────┐
   │      Trading Engine Layer           │
   │                                      │
   │  ┌──────────────────────────────┐  │
   │  │  Brokers (Multi-Asset)       │  │
   │  │  - Bybit    (Crypto)         │  │
   │  │  - Binance  (Crypto)         │  │
   │  │  - MEXC     (Crypto)         │  │
   │  │  - Alpaca   (Stocks/Crypto)  │  │
   │  │  - TradeStation (Stocks)     │  │
   │  │  - IBKR     (Multi-Asset)    │  │
   │  └──────────────────────────────┘  │
   │                                      │
   │  ┌──────────────────────────────┐  │
   │  │  Strategies                   │  │
   │  │  - RSI                        │  │
   │  │  - MACD                       │  │
   │  │  - Momentum                   │  │
   │  │  - MFI                        │  │
   │  │  - VWAP                       │  │
   │  │  - MEM AI (Self-Learning)    │  │
   │  └──────────────────────────────┘  │
   │                                      │
   │  ┌──────────────────────────────┐  │
   │  │  Risk Management              │  │
   │  │  - Position Sizing            │  │
   │  │  - Stop Loss/Take Profit      │  │
   │  │  - Portfolio Limits           │  │
   │  │  - Correlation Analysis       │  │
   │  └──────────────────────────────┘  │
   └──────────────────────────────────────┘

   ┌─────────────────────────────────────┐
   │    Backtesting Engine (Quad)         │
   │                                      │
   │  ┌────────────┐  ┌────────────┐    │
   │  │QuantConnect│  │ Local LEAN │    │
   │  │   Cloud    │  │   Docker   │    │
   │  └────────────┘  └────────────┘    │
   │                                      │
   │  ┌────────────┐  ┌────────────┐    │
   │  │ Custom C#  │  │Backtesting │    │
   │  │   Engine   │  │    .py     │    │
   │  └────────────┘  └────────────┘    │
   └──────────────────────────────────────┘

   ┌─────────────────────────────────────┐
   │     ML/AI Services Layer             │
   │                                      │
   │  ┌────────────────────────────────┐ │
   │  │  MEM AI (Python FastAPI)       │ │
   │  │  - Trend Reversal Prediction   │ │
   │  │  - Model Training & Monitoring │ │
   │  │  - Drift Detection             │ │
   │  │  - Auto-Retraining             │ │
   │  │  Accuracy: 78%                 │ │
   │  └────────────────────────────────┘ │
   │                                      │
   │  ┌────────────────────────────────┐ │
   │  │  Indicator Service (.NET)      │ │
   │  │  - 100+ Technical Indicators   │ │
   │  │  - Custom Formulas             │ │
   │  └────────────────────────────────┘ │
   └──────────────────────────────────────┘

   ┌─────────────────────────────────────┐
   │       Monitoring & Logging           │
   │                                      │
   │  ┌────────┐  ┌────────┐  ┌────────┐│
   │  │ Serilog│  │  Seq   │  │Metrics ││
   │  │ (Logs) │  │ (View) │  │ (API)  ││
   │  └────────┘  └────────┘  └────────┘│
   │                                      │
   │  ┌────────────────────────────────┐ │
   │  │  Prometheus-Compatible Metrics │ │
   │  └────────────────────────────────┘ │
   └──────────────────────────────────────┘
```

## Data Flow Diagram

### Trading Flow

```
User Request → Frontend → Nginx → API → Validation
                                        ↓
                         Risk Checks ← Trading Engine
                                        ↓
                                   Order Created
                                        ↓
                         ClientOrderId (Idempotency)
                                        ↓
                         Broker Selection (Default: Bybit)
                                        ↓
                         ┌──────────────┴──────────────┐
                         │                             │
                    Bybit API                    QuestDB Storage
                         │                             │
                    Order Placed                  Order Record
                         │                             │
                    Fill Received                Update Status
                         │                             │
                    Webhook Sent                  Event Log
                         │                             │
                    ┌────┴────┐                  ┌────┴────┐
               Discord   Slack                  Metrics   Alerts
```

### Backtesting Flow

```
User Request → API → Backtest Service
                          ↓
                    Engine Selection
                          ↓
          ┌───────────────┼───────────────┐
          │               │               │
    QuantConnect    Local LEAN    Backtesting.py
          │               │               │
    Cloud Compute   Docker Container  Python Engine
          │               │               │
          └───────────────┼───────────────┘
                          ↓
                    Results Aggregation
                          ↓
                    MEM AI Analysis
                          ↓
                    Return to User
                          ↓
                    Store in QuestDB
```

### ML Training Flow

```
Historical Data (QuestDB)
          ↓
    Feature Engineering
          ↓
    MEM AI Service (Python)
          ↓
    Model Training (LSTM/GRU)
          ↓
    ┌─────┴─────┐
    │           │
Validation   Metrics
    │           │
    └─────┬─────┘
          ↓
    Drift Detection
          ↓
    Auto-Retrain if needed
          ↓
    Model Deployed
          ↓
    Predictions Available
```

## Component Interaction Diagram

```
┌─────────────────────────────────────────────────────────┐
│                    Client Layer                         │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐            │
│  │  Web UI  │  │  Mobile  │  │   API    │            │
│  │ (React)  │  │  (TBD)   │  │ Clients  │            │
│  └────┬─────┘  └────┬─────┘  └────┬─────┘            │
└───────┼─────────────┼─────────────┼────────────────────┘
        │             │             │
        └─────────────┴─────────────┘
                      │
                      ▼
        ┌─────────────────────────────┐
        │  Authentication & Security   │
        │  - JWT Tokens                │
        │  - MFA (TOTP)                │
        │  - API Keys                  │
        │  - Rate Limiting             │
        └─────────────┬────────────────┘
                      │
                      ▼
        ┌─────────────────────────────┐
        │  Business Logic Layer        │
        │  ┌────────┐  ┌────────┐     │
        │  │Trading │  │Backtest│     │
        │  │Engine  │  │Service │     │
        │  └────┬───┘  └────┬───┘     │
        │       │           │          │
        │  ┌────┴───────────┴───┐     │
        │  │   Strategy Engine   │     │
        │  │  (Signal Generator) │     │
        │  └─────────┬───────────┘     │
        └────────────┼──────────────────┘
                     │
        ┌────────────┼──────────────┐
        │            ▼              │
        │  ┌──────────────────┐    │
        │  │  Risk Management │    │
        │  │  - Validation    │    │
        │  │  - Position Size │    │
        │  │  - Stop Loss     │    │
        │  └──────────────────┘    │
        └──────────────────────────┘
                     │
                     ▼
        ┌─────────────────────────────┐
        │   Data Access Layer          │
        │  ┌────────┐  ┌────────┐     │
        │  │QuestDB │  │ Cache  │     │
        │  │ (TSDB) │  │(Redis) │     │
        │  └────────┘  └────────┘     │
        └─────────────────────────────┘
```

## Deployment Architecture

### Production Deployment

```
┌──────────────────────────────────────────────────────┐
│              Cloud Infrastructure (AWS/Azure)         │
│                                                       │
│  ┌────────────────────────────────────────────────┐ │
│  │           Load Balancer (ALB/Azure LB)         │ │
│  └───────────────────┬────────────────────────────┘ │
│                      │                                │
│      ┌───────────────┼───────────────┐               │
│      │               │               │               │
│  ┌───▼───┐      ┌───▼───┐      ┌───▼───┐          │
│  │ API 1 │      │ API 2 │      │ API 3 │          │
│  │ Node  │      │ Node  │      │ Node  │          │
│  └───┬───┘      └───┬───┘      └───┬───┘          │
│      │              │              │               │
│      └──────────────┼──────────────┘               │
│                     │                               │
│  ┌──────────────────▼─────────────────────────┐   │
│  │         QuestDB Cluster (HA)               │   │
│  │  ┌──────┐  ┌──────┐  ┌──────┐             │   │
│  │  │Node 1│  │Node 2│  │Node 3│             │   │
│  │  └──────┘  └──────┘  └──────┘             │   │
│  └────────────────────────────────────────────┘   │
│                                                     │
│  ┌────────────────────────────────────────────┐   │
│  │         Object Storage (S3/Blob)           │   │
│  │  - Backtest Results                        │   │
│  │  - ML Models                               │   │
│  │  - Logs Archives                           │   │
│  └────────────────────────────────────────────┘   │
└───────────────────────────────────────────────────┘
```

## Security Architecture

```
┌─────────────────────────────────────────┐
│       External Threats                  │
└─────────────┬───────────────────────────┘
              │
              ▼
┌─────────────────────────────────────────┐
│     Cloudflare / WAF                    │
│  - DDoS Protection                      │
│  - Bot Detection                        │
└─────────────┬───────────────────────────┘
              │
              ▼
┌─────────────────────────────────────────┐
│     SSL/TLS Termination                 │
│  - TLS 1.3                              │
│  - Certificate Auto-Renewal             │
└─────────────┬───────────────────────────┘
              │
              ▼
┌─────────────────────────────────────────┐
│     Nginx Security Layer                │
│  - Rate Limiting (IP & Client)          │
│  - Request Validation                   │
│  - Header Injection Protection          │
└─────────────┬───────────────────────────┘
              │
              ▼
┌─────────────────────────────────────────┐
│     API Security Middleware             │
│  - JWT Validation                       │
│  - API Key Verification                 │
│  - MFA Challenge                        │
│  - CORS Policy                          │
└─────────────┬───────────────────────────┘
              │
              ▼
┌─────────────────────────────────────────┐
│     Input Validation                    │
│  - Schema Validation                    │
│  - SQL Injection Prevention             │
│  - XSS Prevention                       │
└─────────────┬───────────────────────────┘
              │
              ▼
┌─────────────────────────────────────────┐
│     Business Logic                      │
│  - Order Idempotency                    │
│  - Risk Validation                      │
│  - Audit Logging                        │
└─────────────────────────────────────────┘
```

## Technology Stack

### Backend
- **.NET 8.0** - API Framework
- **ASP.NET Core** - Web Framework
- **C#** - Programming Language
- **Bybit.Net / Binance.Net** - Exchange Clients
- **Serilog** - Structured Logging
- **xUnit** - Testing Framework

### Frontend
- **React 18** - UI Library
- **Vite** - Build Tool
- **TypeScript** - Type Safety
- **TailwindCSS** - Styling
- **Recharts** - Charting

### Data Storage
- **QuestDB** - Time-Series Database
- **Redis** - Caching Layer
- **PostgreSQL** - Relational Data (via QuestDB)

### ML/AI
- **Python 3.11** - ML Runtime
- **FastAPI** - ML Service API
- **TensorFlow/PyTorch** - ML Frameworks
- **Pandas/NumPy** - Data Processing

### DevOps
- **Docker** - Containerization
- **Docker Compose** - Local Orchestration
- **GitHub Actions** - CI/CD
- **Nginx** - Reverse Proxy

### Monitoring
- **Seq** - Log Aggregation
- **Prometheus** - Metrics (Compatible)
- **Serilog** - Structured Logging

## Scalability Patterns

### Horizontal Scaling
- API nodes can scale independently
- Load balancer distributes requests
- Stateless design enables easy scaling

### Vertical Scaling
- QuestDB performance tuning
- Increased memory for ML models
- SSD for faster I/O

### Caching Strategy
- Redis for frequently accessed data
- Client-side caching for static data
- CDN for frontend assets

## High Availability

### Redundancy
- Multiple API instances
- Database replication
- Backup brokers for critical operations

### Failover
- Automatic health checks
- Circuit breakers for external services
- Graceful degradation

### Disaster Recovery
- Regular backups (hourly)
- Point-in-time recovery
- Geographic redundancy (optional)

---

**Last Updated:** October 21, 2025
**Version:** 2.6.0
**Maintained By:** AlgoTrendy Development Team
