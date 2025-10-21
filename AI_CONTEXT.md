# AI Context - AlgoTrendy v2.6

**Last Updated**: 2025-10-21
**Version**: 2.6.0
**Status**: Production Ready (98/100) | Security: 98.5/100 | Tests: 75% (306/407)

---

## Quick Overview

**AlgoTrendy** is an enterprise-grade algorithmic trading platform built with .NET 8.0 that integrates multiple brokers, provides institutional-quality backtesting, and leverages AI (MEM - Memory-Enhanced Machine Learning) for intelligent trading analysis.

**Repository**: `/root/AlgoTrendy_v2.6`
**Git Branch**: `main` (clean working tree)
**Architecture**: Modular Monolith with Clean Architecture principles

---

## Project Structure

```
AlgoTrendy_v2.6/
├── backend/                           # .NET 8 Backend (Primary)
│   ├── AlgoTrendy.API/                # REST API & Controllers (Entry Point)
│   ├── AlgoTrendy.Core/               # Domain Models & Interfaces
│   ├── AlgoTrendy.TradingEngine/      # Trading Logic & Broker Integrations
│   ├── AlgoTrendy.DataChannels/       # Market Data Channels & Providers
│   ├── AlgoTrendy.Backtesting/        # Backtesting Engines
│   ├── AlgoTrendy.Infrastructure/     # Data Access & External Services
│   ├── AlgoTrendy.Common.Abstractions/ # Shared Abstractions
│   └── AlgoTrendy.Tests/              # xUnit Tests
├── MEM/                               # AI Memory System (Python)
├── frontend/                          # React/TypeScript UI (Vite)
├── services/                          # Microservices (future)
├── docs/                              # Documentation (50+ pages)
├── docker-compose.yml                 # Container Orchestration
└── scripts/                           # Automation Scripts
```

---

## Technology Stack

### Backend
- **.NET**: 8.0
- **Framework**: ASP.NET Core 8.0
- **ORM**: Entity Framework Core
- **Authentication**: JWT + MFA/TOTP
- **Logging**: Serilog + Seq
- **Real-time**: SignalR
- **Testing**: xUnit, Moq, FluentAssertions

### Databases
- **QuestDB**: Time-series data (primary) - PostgreSQL wire protocol
- **PostgreSQL**: Metadata (optional fallback)
- **Redis**: Caching & message broker

### Active Brokers (5)
1. **Binance** - Via Binance.Net package
2. **Bybit** - Primary crypto broker
3. **Coinbase** - AdvancedTrade API
4. **Interactive Brokers** - IBKR Gateway integration
5. **TradeStation** - REST API integration

### Disabled Brokers (2)
- **MEXCBroker** - Implementation incomplete (needs completion)
- **KrakenBroker** - Package API mismatch (needs REST implementation)

### Market Data Providers (FREE)
- Alpha Vantage, Yahoo Finance, Tiingo, Polygon.io, Alpaca, TwelveData, EODHD, CoinGecko, Finnhub

---

## Key Components & File Paths

### API Layer
**Location**: `backend/AlgoTrendy.API/`

**Entry Point**: `Program.cs` - API configuration, DI registration, middleware setup

**Controllers** (11 total):
- `Controllers/BacktestingController.cs` - Backtest execution & analysis
- `Controllers/CryptoDataController.cs` - Cryptocurrency market data
- `Controllers/MarketDataController.cs` - General market data endpoints
- `Controllers/MfaController.cs` - Multi-factor authentication
- `Controllers/PortfolioAnalyticsController.cs` - Portfolio analysis
- `Controllers/PortfolioController.cs` - Portfolio management
- `Controllers/QuantConnectController.cs` - QuantConnect integration
- `Controllers/TradingController.cs` - Trading operations
- `Controllers/OrdersController.cs` - Order management
- `Controllers/PositionsController.cs` - Position tracking
- `Controllers/MLTrainingController.cs` - ML model training endpoints

**Middleware**:
1. Security headers
2. CORS configuration
3. Request correlation ID
4. JWT authentication
5. Request logging (Serilog)
6. Rate limiting (IP & client)
7. Authorization

### Trading Engine
**Location**: `backend/AlgoTrendy.TradingEngine/`

**Broker Implementations**:
- `Brokers/BrokerBase.cs` - Abstract base class
- `Brokers/BinanceBroker.cs` - Binance integration (ACTIVE)
- `Brokers/BybitBroker.cs` - Bybit (ACTIVE)
- `Brokers/CoinbaseBroker.cs` - Coinbase (ACTIVE)
- `Brokers/InteractiveBrokersBroker.cs` - IBKR (ACTIVE)
- `Brokers/TradeStationBroker.cs` - TradeStation (ACTIVE)
- `Brokers/NinjaTraderBroker.cs` - NinjaTrader (ACTIVE)
- `Brokers/MEXCBroker.cs` - MEXC (DISABLED - incomplete)

**Trading Strategies**:
- `Strategies/RSIStrategy.cs` - Relative Strength Index
- `Strategies/MACDStrategy.cs` - MACD indicator
- `Strategies/MomentumStrategy.cs` - Momentum-based
- `Strategies/VWAPStrategy.cs` - Volume Weighted Average Price
- `Strategies/MFIStrategy.cs` - Money Flow Index

**Services**:
- `Services/RiskAnalyticsService.cs` - Risk calculations & monitoring
- `Services/PortfolioOptimizationService.cs` - Portfolio allocation
- `Services/IndicatorService.cs` - Technical indicators
- `Services/MLFeatureService.cs` - ML feature engineering
- `Services/StrategyFactory.cs` - Strategy instantiation
- `Services/TradingEngine.cs` - Core trading logic

### Backtesting Module
**Location**: `backend/AlgoTrendy.Backtesting/`

**Engines**:
- `Engines/CustomBacktestEngine.cs` - DISABLED (awaiting accuracy verification)
- `Engines/QuantConnectBacktestEngine.cs` - Cloud-based institutional backtesting (ACTIVE)
- `Engines/LocalLeanBacktestEngine.cs` - Local LEAN algorithm framework (ACTIVE)
- `Engines/BacktestingPyEngine.cs` - Python-based backtesting service (ACTIVE)

**Services**:
- `Services/BacktestService.cs` - Orchestration & result aggregation
- `Services/QuantConnectApiClient.cs` - QuantConnect REST client
- `Services/BacktestEngineFactory.cs` - Engine selection & instantiation
- `Services/MEMIntegrationService.cs` - AI-powered backtest analysis

### Data Channels
**Location**: `backend/AlgoTrendy.DataChannels/`

**Data Providers**:
- `DataProviders/AlphaVantageProvider.cs`
- `DataProviders/YFinanceProvider.cs`
- `DataProviders/TiingoProvider.cs`
- `DataProviders/PolygonProvider.cs`
- `DataProviders/AlpacaProvider.cs`
- `DataProviders/TwelveDataProvider.cs`
- `DataProviders/EODHDProvider.cs`
- `DataProviders/CoinGeckoProvider.cs`

**REST Channels**:
- `RestChannels/BinanceRestChannel.cs`
- `RestChannels/OKXRestChannel.cs`
- `RestChannels/CoinbaseRestChannel.cs`
- `RestChannels/KrakenRestChannel.cs`

### Core Domain
**Location**: `backend/AlgoTrendy.Core/`

**Key Models**:
- `Models/Order.cs` - Order data model
- `Models/Position.cs` - Trading position with P&L
- `Models/Trade.cs` - Executed trade record
- `Models/MarketData.cs` - Real-time market data
- `Models/User.cs` - User account
- `Models/UserMfaSettings.cs` - MFA configuration

**Key Interfaces**:
- `Interfaces/IBroker.cs` - Broker abstraction
- `Interfaces/IStrategy.cs` - Trading strategy interface
- `Interfaces/IDataProvider.cs` - Market data provider
- `Interfaces/IBacktestEngine.cs` - Backtesting engine
- `Interfaces/IMarketDataRepository.cs` - Data persistence
- `Interfaces/IOrderRepository.cs` - Order persistence
- `Interfaces/IPositionRepository.cs` - Position persistence

### Infrastructure
**Location**: `backend/AlgoTrendy.Infrastructure/`

**Repositories**:
- `Repositories/MarketDataRepository.cs` - QuestDB time-series storage
- `Repositories/OrderRepository.cs` - Order persistence
- `Repositories/PositionRepository.cs` - Position tracking

**External Services**:
- `Services/FinnhubService.cs` - Cryptocurrency market data
- `Services/OFACScreeningService.cs` - Anti-money laundering
- `Services/AzureKeyVaultSecretsService.cs` - Secret management
- `Services/TradeSurveillanceService.cs` - Trade monitoring
- `Services/RegulatoryReportingService.cs` - SEC/FINRA reporting

### MEM AI System
**Location**: `MEM/`

**Core Components**:
- `mem_connector.py` - MemGPT agent integration
- `mem_connection_manager.py` - Multi-broker connection management
- `mem_credentials.py` - Secure credential handling
- `mem_live_dashboard.py` - Real-time monitoring UI
- `singleton_decorator.py` - Instance management

**ML Models**:
- `models/reversal_model.joblib` - Gradient Boosting (78% accuracy)
- `models/reversal_scaler.joblib` - Feature normalization
- `models/config.json` - Model configuration
- `models/model_metrics.json` - Performance tracking

**Memory System**:
- `memory/core_memory_updates.txt` - Decision history & patterns
- `memory/parameter_updates.json` - Parameter tuning log
- `strategies/strategy_modules.py` - Auto-generated strategies (15 active)

### Tests
**Location**: `backend/AlgoTrendy.Tests/`

**Test Categories**:
- `Unit/Strategies/` - Technical indicator tests
- `Unit/Core/` - Domain model tests
- `Unit/TradingEngine/` - Trading logic tests
- `Unit/Infrastructure/` - Repository tests
- `Integration/` - API & broker integration tests

**Status**: 306/407 passing (75% coverage)

---

## Current State & Known Issues

### What Works ✅
1. Trading execution across 5 brokers
2. Real-time & historical market data (9+ providers)
3. QuantConnect backtesting integration
4. Enterprise-grade security (98.5/100)
5. 28+ REST API endpoints with Swagger
6. SignalR WebSocket real-time updates
7. JWT + MFA/TOTP authentication
8. QuestDB time-series database
9. Serilog + Seq logging infrastructure

### What's Disabled ⚠️

#### 1. CustomBacktestEngine
- **File**: `backend/AlgoTrendy.Backtesting/Engines/CustomBacktestEngine.cs`
- **Reason**: Needs accuracy validation against QuantConnect
- **Status**: Registered for DI but returns error at runtime
- **Fix Required**: Validate metrics calculations and enable

#### 2. MEXCBroker
- **File**: `backend/AlgoTrendy.TradingEngine/Brokers/MEXCBroker.cs`
- **Reason**: Implementation incomplete
- **Status**: Not registered in dependency injection
- **Fix Required**: Complete implementation, add to Program.cs DI registration
- **Related**: MEXC.Net package installed but needs proper integration

#### 3. KrakenBroker
- **File**: `backend/AlgoTrendy.TradingEngine/Brokers/KrakenBroker.cs` (if exists)
- **Reason**: Package API mismatch with actual Kraken requirements
- **Status**: Commented out in Program.cs
- **Fix Required**: Implement REST API directly instead of relying on package

### Known Issues
1. **Frontend**: Limited functionality - mostly skeleton components
2. **Test Coverage**: 75% (306/407) - some integration tests need broker credentials
3. **Binance.Net**: Needs upgrade from v10.x to v11.x
4. **Documentation**: Comprehensive but scattered across 50+ files

---

## Common Tasks & How to Do Them

### Building & Running

```bash
# Build entire solution
cd /root/AlgoTrendy_v2.6/backend
dotnet build

# Run API
cd /root/AlgoTrendy_v2.6/backend/AlgoTrendy.API
dotnet run

# Run tests
cd /root/AlgoTrendy_v2.6/backend
dotnet test

# Run specific test category
dotnet test --filter "CategoryName=Unit"

# With coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Docker Deployment

```bash
cd /root/AlgoTrendy_v2.6

# Copy environment template
cp .env.example .env

# Edit .env with credentials
nano .env

# Start all services
docker-compose up -d

# Check logs
docker-compose logs -f api

# Stop services
docker-compose down
```

### Adding a New Broker

1. Create new broker class in `backend/AlgoTrendy.TradingEngine/Brokers/`
2. Inherit from `BrokerBase`
3. Implement required methods: `PlaceOrderAsync`, `GetPositionsAsync`, `GetOrdersAsync`, etc.
4. Add NuGet package if available
5. Register in `backend/AlgoTrendy.API/Program.cs`:
   ```csharp
   services.AddSingleton<IBroker, YourNewBroker>();
   ```
6. Add configuration to `.env`
7. Add tests in `backend/AlgoTrendy.Tests/Unit/TradingEngine/`

### Adding a New Strategy

1. Create new strategy class in `backend/AlgoTrendy.TradingEngine/Strategies/`
2. Implement `IStrategy` interface
3. Add strategy logic in `GenerateSignalsAsync` method
4. Register in `backend/AlgoTrendy.TradingEngine/Services/StrategyFactory.cs`
5. Add tests in `backend/AlgoTrendy.Tests/Unit/Strategies/`

### Adding a New API Endpoint

1. Add method to appropriate controller in `backend/AlgoTrendy.API/Controllers/`
2. Add DTO classes in `backend/AlgoTrendy.API/DTOs/` if needed
3. Use `[Authorize]` attribute if authentication required
4. Add `[ProducesResponseType]` attributes for Swagger
5. Add integration test in `backend/AlgoTrendy.Tests/Integration/`

### Running Security Scans

```bash
cd /root/AlgoTrendy_v2.6/file_mgmt_code

# Setup pre-commit hooks (one-time)
./setup-precommit-hooks.sh

# Run security scan manually
./scan-security.sh
```

### Accessing MEM AI Dashboard

```bash
cd /root/AlgoTrendy_v2.6/MEM
python mem_live_dashboard.py

# Access at http://localhost:5001
```

---

## Design Patterns & Conventions

### Design Patterns Used
1. **Repository Pattern** - Data access abstraction (`Infrastructure/Repositories/`)
2. **Factory Pattern** - Strategy & engine instantiation (`Services/*Factory.cs`)
3. **Strategy Pattern** - Trading strategies (`TradingEngine/Strategies/`)
4. **Singleton Pattern** - Service instances (DI-managed)
5. **Dependency Injection** - Built-in ASP.NET Core DI
6. **Service Locator** - Broker resolution via DI

### Code Conventions

#### Naming
- **Classes**: PascalCase (`BinanceBroker`, `MarketDataRepository`)
- **Methods**: PascalCase (`PlaceOrderAsync`, `GetPositionsAsync`)
- **Variables**: camelCase (`orderRequest`, `position`)
- **Interfaces**: IPascalCase (`IBroker`, `IStrategy`)
- **Private fields**: _camelCase (`_logger`, _httpClient`)

#### Commit Messages
Follow conventional commits:
- `feat:` - New features
- `fix:` - Bug fixes
- `docs:` - Documentation changes
- `refactor:` - Code refactoring
- `security:` - Security improvements
- `chore:` - Maintenance tasks
- `test:` - Test additions/changes

#### Async/Await
- All I/O operations use async/await
- Methods end with `Async` suffix
- Return `Task` or `Task<T>`

#### Error Handling
```csharp
try
{
    // Operation
}
catch (Exception ex)
{
    _logger.LogError(ex, "Error message with context");
    throw; // or return error response
}
```

#### XML Documentation
```csharp
/// <summary>
/// Brief description
/// </summary>
/// <param name="paramName">Parameter description</param>
/// <returns>Return value description</returns>
public async Task<Order> PlaceOrderAsync(OrderRequest request)
{
    // Implementation
}
```

---

## Configuration

### Environment Variables (.env)

```bash
# Database
DATABASE_TYPE=questdb
QUESTDB_CONNECTION=Host=localhost;Port=8812;Database=qdb;Username=admin;Password=quest
POSTGRES_CONNECTION=Host=localhost;Port=5432;Database=algotrendy;Username=postgres;Password=your_password

# Authentication
JWT_SECRET=your-secret-key-min-32-chars
JWT_ISSUER=AlgoTrendy
JWT_AUDIENCE=AlgoTrendy

# Brokers
BINANCE_API_KEY=your-api-key
BINANCE_API_SECRET=your-api-secret
BYBIT_API_KEY=your-api-key
BYBIT_API_SECRET=your-api-secret
COINBASE_API_KEY=your-api-key
COINBASE_API_SECRET=your-api-secret
IBKR_HOST=localhost
IBKR_PORT=4001
IBKR_CLIENT_ID=1

# Data Providers
ALPHA_VANTAGE_API_KEY=your-api-key
FINNHUB_API_KEY=your-api-key
COINGECKO_API_KEY=your-api-key

# AI Services
OPENAI_API_KEY=your-api-key
ANTHROPIC_API_KEY=your-api-key
MEMGPT_API_KEY=your-api-key

# Logging
SEQ_URL=http://localhost:5341
```

### Dependency Injection (Program.cs)

```csharp
// Brokers
services.AddSingleton<IBroker, BinanceBroker>();
services.AddSingleton<IBroker, BybitBroker>();
services.AddSingleton<IBroker, CoinbaseBroker>();
services.AddSingleton<IBroker, InteractiveBrokersBroker>();
services.AddSingleton<IBroker, TradeStationBroker>();

// Data Providers
services.AddSingleton<IDataProvider, AlphaVantageProvider>();
services.AddSingleton<IDataProvider, YFinanceProvider>();

// Backtesting Engines
services.AddSingleton<IBacktestEngine, QuantConnectBacktestEngine>();
services.AddSingleton<IBacktestEngine, LocalLeanBacktestEngine>();

// Services
services.AddSingleton<BacktestService>();
services.AddSingleton<TradingEngine>();
services.AddSingleton<RiskAnalyticsService>();
```

---

## Important Reminders for AI Assistants

### When Making Changes

1. **ALWAYS read files before editing** - Use Read tool first
2. **Preserve exact indentation** - Match existing code style
3. **Follow async/await patterns** - All I/O should be async
4. **Add XML documentation** - Document public APIs
5. **Update tests** - Add/modify tests for changes
6. **Check DI registration** - Ensure new services are registered in Program.cs
7. **Update CHANGELOG** - Document changes in module CHANGELOG.md

### Security Considerations

1. **NEVER commit secrets** - Use environment variables
2. **Run security scans** - Before committing: `./scan-security.sh`
3. **Use pre-commit hooks** - Gitleaks + Semgrep enabled
4. **Validate inputs** - Prevent SQL injection, XSS
5. **Log sensitive operations** - Audit trail for compliance
6. **Rate limiting** - Respect broker API limits

### When Adding Features

1. **Check existing implementations** - Look for similar features first
2. **Follow Clean Architecture** - Keep layers separated
3. **Use dependency injection** - Don't create instances directly
4. **Add integration tests** - Test API endpoints end-to-end
5. **Update Swagger docs** - Add `[ProducesResponseType]` attributes
6. **Consider MEM integration** - Can AI enhance this feature?

### When Debugging

1. **Check logs** - Serilog writes to console and Seq
2. **Enable debug logging** - Set log level in appsettings.Development.json
3. **Use breakpoints** - VS Code debugging works well
4. **Check QuestDB** - Verify data in QuestDB console (port 9000)
5. **Review broker responses** - Log API responses for debugging
6. **Check DI registration** - Service not found? Check Program.cs

### File Locations Quick Reference

| Need to... | Look in... |
|------------|-----------|
| Add API endpoint | `backend/AlgoTrendy.API/Controllers/` |
| Add broker | `backend/AlgoTrendy.TradingEngine/Brokers/` |
| Add strategy | `backend/AlgoTrendy.TradingEngine/Strategies/` |
| Add data provider | `backend/AlgoTrendy.DataChannels/DataProviders/` |
| Add domain model | `backend/AlgoTrendy.Core/Models/` |
| Add repository | `backend/AlgoTrendy.Infrastructure/Repositories/` |
| Add test | `backend/AlgoTrendy.Tests/Unit/` or `Integration/` |
| Configure DI | `backend/AlgoTrendy.API/Program.cs` |
| Add middleware | `backend/AlgoTrendy.API/Program.cs` |
| Update docs | `docs/` |

---

## Recent Development Activity

### Latest Commits
```
0272581 feat: Add automated release infrastructure (GitVersion + semantic-release)
0c7df8f feat: Add module versioning infrastructure with GitVersion
89cfb78 fix: Disable incomplete MEXCBroker to restore build
6a54cdf docs: Add comprehensive architecture and versioning strategy
8ccb8bb docs: Update all documentation with comprehensive security information
```

### Current Development Focus
- Automated release infrastructure (semantic-release + GitHub Actions)
- Module versioning with GitVersion
- Security hardening (98.5/100 score achieved)
- Architecture documentation for Phase 3 parallel transition

### Immediate Priorities
1. Complete MEXCBroker implementation
2. Fix KrakenBroker (REST API approach)
3. Validate and enable CustomBacktestEngine
4. Upgrade Binance.Net to v11.x
5. Enhance frontend UI components

---

## Performance Metrics

### System Performance
- **API Response Time**: <100ms (95th percentile)
- **Order Execution**: <500ms average
- **Backtest Speed**: 1M bars/minute (QuantConnect)
- **Market Data Latency**: <50ms (WebSocket)

### MEM AI Impact
- **Win Rate**: 48% → 62.5% (+30%)
- **Avg Gain**: +0.8% → +1.2% (+50%)
- **Sharpe Ratio**: 1.2 → 2.1 (+75%)
- **Max Drawdown**: -5.2% → -2.3% (-56%)
- **ML Accuracy**: 78% (reversal prediction)

### Cost Savings
- **Market Data**: $0/month (was $5,100/month)
- **Annual Savings**: $61,200+ via free-tier providers
- **Infrastructure**: Docker-based, low overhead

---

## Support & Resources

### Documentation
- **Main README**: `/root/AlgoTrendy_v2.6/README.md`
- **Quick Start**: `/root/AlgoTrendy_v2.6/QUICK_START_GUIDE.md`
- **Architecture Map**: `/root/AlgoTrendy_v2.6/ARCHITECTURE_MAP.md`
- **Security Policy**: `/root/AlgoTrendy_v2.6/SECURITY.md`
- **Developer Onboarding**: `/root/AlgoTrendy_v2.6/DEVELOPER_ONBOARDING.md`
- **MEM Architecture**: `/root/AlgoTrendy_v2.6/MEM/MEM_ARCHITECTURE.md`

### API Documentation
- **Swagger UI**: http://localhost:5002/swagger (when running)
- **OpenAPI Spec**: http://localhost:5002/swagger/v1/swagger.json

### Database Access
- **QuestDB Console**: http://localhost:9000
- **Seq Logs**: http://localhost:5341

### Monitoring
- **MEM Dashboard**: http://localhost:5001 (when running)
- **Health Checks**: http://localhost:5002/health

---

## Summary

AlgoTrendy v2.6 is a production-ready algorithmic trading platform with:
- **5 active brokers** + 2 disabled (needs fixes)
- **9+ free market data providers** ($61K+/year savings)
- **Institutional backtesting** via QuantConnect
- **AI-enhanced trading** via MEM system (78% ML accuracy)
- **Enterprise security** (98.5/100 score)
- **Clean architecture** with .NET 8.0 best practices
- **Comprehensive testing** (75% coverage, 306/407 tests)
- **Docker-ready deployment** for easy scaling

**Current Status**: Production-ready with clear roadmap for enhancements. Focus areas: complete disabled features, upgrade dependencies, enhance frontend.

---

*This document serves as the primary AI context for understanding and working with the AlgoTrendy v2.6 codebase.*
