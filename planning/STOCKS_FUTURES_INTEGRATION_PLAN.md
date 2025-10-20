# Stocks & Futures Integration Plan

**Goal:** Enable trading of stocks, futures, and options on AlgoTrendy v2.6

**Estimated Time:** 8-12 hours

**Date:** October 20, 2025

---

## Current Status Assessment

### ✅ What We Have

**Data Infrastructure:**
- Alpha Vantage provider (200K+ US stocks, FREE tier)
- yfinance provider (300K+ symbols including stocks, options, forex)
- yfinance Python service running on port 5001
- Full options chain support with Greeks

**Broker Infrastructure:**
- Interactive Brokers broker (stocks, futures, options, forex)
- NinjaTrader broker (futures)
- TradeStation broker (stocks, options)
- Binance broker (crypto - fully operational)
- Bybit broker (crypto futures - fully operational)

**v2.5 Reference Code:**
- Asset class definitions (crypto, stocks, futures, ETFs)
- Symbol formatting logic for different brokers
- Alpaca broker integration (stocks/ETFs)

### ❌ What's Missing

1. **AssetType/AssetClass enum** - Need to distinguish crypto/stocks/futures
2. **Stock data channel integration** - FREE tier not connected to MarketDataChannelService
3. **Futures data channel integration** - No futures-specific data channel
4. **Broker symbol formatting** - Need to handle stock/futures symbols properly
5. **API endpoints** - Stock/futures-specific endpoints
6. **Configuration** - Broker credentials for stock/futures brokers
7. **End-to-end testing** - Validate complete trading flow

---

## Implementation Steps

### **Phase 1: Core Infrastructure (2-3 hours)**

#### 1.1 Add AssetType Enum
**File:** `backend/AlgoTrendy.Core/Enums/AssetType.cs`

```csharp
namespace AlgoTrendy.Core.Enums;

/// <summary>
/// Asset class types
/// </summary>
public enum AssetType
{
    Cryptocurrency = 0,
    Stock = 1,
    Futures = 2,
    Options = 3,
    ETF = 4,
    Forex = 5
}
```

#### 1.2 Add AssetType to MarketData Model
**File:** `backend/AlgoTrendy.Core/Models/MarketData.cs`

Add property:
```csharp
public AssetType AssetType { get; set; } = AssetType.Cryptocurrency;
```

#### 1.3 Create Symbol Formatter Service
**File:** `backend/AlgoTrendy.Core/Services/SymbolFormatterService.cs`

```csharp
public class SymbolFormatterService
{
    public string FormatForBroker(string symbol, AssetType assetType, string brokerName)
    {
        // Interactive Brokers: AAPL (stocks), ES (futures)
        // TradeStation: AAPL (stocks)
        // NinjaTrader: ES 12-25 (futures)
        // Bybit: BTCUSDT (crypto), BTCUSD (futures)
        // etc.
    }
}
```

---

### **Phase 2: Stock Data Integration (2-3 hours)**

#### 2.1 Create Stock Data Channel
**File:** `backend/AlgoTrendy.DataChannels/Channels/REST/StockDataChannel.cs`

```csharp
public class StockDataChannel : IMarketDataChannel
{
    private readonly IMarketDataProvider _alphaVantageProvider;
    private readonly IMarketDataProvider _yfinanceProvider;

    // Fetch data for configured stock symbols
    // Use Alpha Vantage for intraday (500 calls/day limit)
    // Use yfinance for historical and daily data (unlimited)
}
```

**Symbols to track initially:**
- AAPL, GOOGL, MSFT, TSLA, AMZN, META, NVDA, AMD
- SPY, QQQ, IWM (ETFs for market indicators)

#### 2.2 Register Stock Data Channel
**File:** `backend/AlgoTrendy.API/Program.cs`

```csharp
// Add stock data providers
builder.Services.AddScoped<AlphaVantageProvider>();
builder.Services.AddScoped<YFinanceProvider>();
builder.Services.AddScoped<StockDataChannel>();
```

#### 2.3 Update MarketDataChannelService
**File:** `backend/AlgoTrendy.DataChannels/Services/MarketDataChannelService.cs`

Add stock channel to fetch loop:
```csharp
var tasks = new List<Task<(string channelName, int recordCount, bool success)>>
{
    FetchFromChannelAsync<BinanceRestChannel>(scope, "Binance", cancellationToken),
    FetchFromChannelAsync<OKXRestChannel>(scope, "OKX", cancellationToken),
    FetchFromChannelAsync<CoinbaseRestChannel>(scope, "Coinbase", cancellationToken),
    FetchFromChannelAsync<KrakenRestChannel>(scope, "Kraken", cancellationToken),
    FetchFromChannelAsync<StockDataChannel>(scope, "Stocks", cancellationToken) // NEW
};
```

---

### **Phase 3: Futures Data Integration (1-2 hours)**

#### 3.1 Create Futures Data Channel
**File:** `backend/AlgoTrendy.DataChannels/Channels/REST/FuturesDataChannel.cs`

```csharp
public class FuturesDataChannel : IMarketDataChannel
{
    // Use existing Bybit broker for crypto futures
    // Can add CME/NYMEX futures through Interactive Brokers data feed

    // Crypto futures: BTCUSD, ETHUSD (Bybit)
    // Traditional futures: ES, NQ, CL, GC (via IB if data feed available)
}
```

**Futures to track initially:**
- BTCUSD, ETHUSD (crypto perpetuals via Bybit)
- ES (S&P 500 E-mini), NQ (Nasdaq E-mini) - if IB data available

---

### **Phase 4: Broker Configuration (2 hours)**

#### 4.1 Interactive Brokers Setup
**File:** `appsettings.json`

```json
{
  "InteractiveBrokers": {
    "GatewayHost": "localhost",
    "GatewayPort": 4001,
    "ClientId": 1,
    "AccountId": "DU123456"
  }
}
```

**Requirements:**
- TWS (Trader Workstation) or IB Gateway must be running
- Enable API connections in TWS settings
- Set socket port (default 4001 for live, 7497 for paper)

#### 4.2 TradeStation Setup
**File:** `appsettings.json`

```json
{
  "TradeStation": {
    "ApiKey": "your_api_key",
    "ApiSecret": "your_api_secret",
    "UsePaperTrading": true
  }
}
```

**Requirements:**
- TradeStation API credentials (OAuth 2.0)
- Paper trading account recommended for testing

#### 4.3 NinjaTrader Setup
**File:** `appsettings.json`

```json
{
  "NinjaTrader": {
    "Host": "localhost",
    "Port": 8080,
    "AccountId": "Sim101",
    "ConnectionType": "rest"
  }
}
```

**Requirements:**
- NinjaTrader 8 platform running
- Enable REST API in NinjaTrader settings
- Use Sim101 account for testing

---

### **Phase 5: API Endpoints (1 hour)**

#### 5.1 Add Stock/Futures Endpoints
**File:** `backend/AlgoTrendy.API/Controllers/MarketDataController.cs`

```csharp
[HttpGet("stocks/{symbol}")]
public async Task<IActionResult> GetStockData(string symbol)
{
    // Fetch stock data from yfinance/Alpha Vantage
}

[HttpGet("futures/{symbol}")]
public async Task<IActionResult> GetFuturesData(string symbol)
{
    // Fetch futures data
}

[HttpGet("options/{symbol}")]
public async Task<IActionResult> GetOptionsChain(string symbol)
{
    // Fetch options chain from yfinance
}

[HttpGet("options/{symbol}/expirations")]
public async Task<IActionResult> GetOptionsExpirations(string symbol)
{
    // Get available expiration dates
}
```

---

### **Phase 6: Trading Workflow Integration (1-2 hours)**

#### 6.1 Update Order Placement
**File:** `backend/AlgoTrendy.TradingEngine/Services/TradingEngine.cs`

```csharp
public async Task<Order> PlaceOrderAsync(OrderRequest request)
{
    // Detect asset type from symbol
    var assetType = DetectAssetType(request.Symbol);

    // Select appropriate broker
    var broker = SelectBroker(assetType);

    // Format symbol for broker
    var formattedSymbol = _symbolFormatter.FormatForBroker(
        request.Symbol, assetType, broker.BrokerName);

    // Place order
    return await broker.PlaceOrderAsync(formattedSymbol, ...);
}
```

#### 6.2 Create Broker Selector
**File:** `backend/AlgoTrendy.TradingEngine/Services/BrokerSelectorService.cs`

```csharp
public class BrokerSelectorService
{
    public IBroker SelectBroker(AssetType assetType, string preferredBroker = null)
    {
        // Cryptocurrency -> Binance or Bybit
        // Stock -> TradeStation or Interactive Brokers
        // Futures -> NinjaTrader or Interactive Brokers
        // Options -> Interactive Brokers or TradeStation
    }
}
```

---

### **Phase 7: Testing (2-3 hours)**

#### 7.1 Stock Trading Test
```bash
# Start yfinance service
python backend/AlgoTrendy.DataChannels/PythonServices/yfinance_service.py

# Test stock quote
curl "http://localhost:5001/latest?symbol=AAPL"

# Place test order (paper trading)
curl -X POST http://localhost:5002/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "symbol": "AAPL",
    "side": "buy",
    "quantity": 1,
    "assetType": "stock"
  }'
```

#### 7.2 Futures Trading Test
```bash
# Test futures quote
curl "http://localhost:5002/api/market-data/futures/BTCUSD"

# Place futures order
curl -X POST http://localhost:5002/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "symbol": "BTCUSD",
    "side": "buy",
    "quantity": 0.01,
    "assetType": "futures"
  }'
```

#### 7.3 Options Trading Test
```bash
# Get options expirations
curl "http://localhost:5001/options/expirations?symbol=AAPL"

# Get options chain
curl "http://localhost:5001/options?symbol=AAPL&expiration=2025-11-21"

# Place options order (if supported by broker)
```

---

## Configuration Example

**Complete appsettings.json:**

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  },

  "MarketData": {
    "FetchIntervalSeconds": 60,
    "EnableBinance": true,
    "EnableOKX": true,
    "EnableCoinbase": true,
    "EnableKraken": true,
    "EnableStocks": true,
    "EnableFutures": true
  },

  "AlphaVantage": {
    "ApiKey": "your_alpha_vantage_key"
  },

  "YFinance": {
    "ServiceUrl": "http://localhost:5001"
  },

  "Binance": {
    "ApiKey": "your_binance_key",
    "ApiSecret": "your_binance_secret",
    "UseTestnet": true
  },

  "Bybit": {
    "ApiKey": "your_bybit_key",
    "ApiSecret": "your_bybit_secret",
    "UseTestnet": true
  },

  "InteractiveBrokers": {
    "GatewayHost": "localhost",
    "GatewayPort": 4001,
    "ClientId": 1,
    "AccountId": "DU123456"
  },

  "TradeStation": {
    "ApiKey": "your_tradestation_key",
    "ApiSecret": "your_tradestation_secret",
    "UsePaperTrading": true
  },

  "NinjaTrader": {
    "Host": "localhost",
    "Port": 8080,
    "AccountId": "Sim101",
    "ConnectionType": "rest"
  }
}
```

---

## Risk Management

### Asset-Specific Limits

```csharp
// appsettings.json
"RiskManagement": {
  "Cryptocurrency": {
    "MaxPositionSizePercent": 10,
    "MaxConcurrentPositions": 3,
    "RequireStopLoss": false
  },
  "Stock": {
    "MaxPositionSizePercent": 20,
    "MaxConcurrentPositions": 10,
    "RequireStopLoss": false
  },
  "Futures": {
    "MaxPositionSizePercent": 5,
    "MaxConcurrentPositions": 2,
    "RequireStopLoss": true
  },
  "Options": {
    "MaxPositionSizePercent": 3,
    "MaxConcurrentPositions": 5,
    "RequireStopLoss": false
  }
}
```

---

## Success Criteria

- [ ] AssetType enum created and integrated
- [ ] Stock data channel operational (yfinance + Alpha Vantage)
- [ ] Futures data channel operational (Bybit perpetuals)
- [ ] Symbol formatter service handles all asset types
- [ ] At least 1 stock broker connected (TradeStation or Interactive Brokers)
- [ ] At least 1 futures broker connected (NinjaTrader or Bybit)
- [ ] API endpoints for stocks/futures/options working
- [ ] End-to-end stock trade test successful
- [ ] End-to-end futures trade test successful
- [ ] Documentation updated
- [ ] Tests passing (unit + integration)

---

## Next Steps After Integration

1. **Paper Trading Validation** - Run for 24-48 hours
2. **Strategy Adaptation** - Adapt existing strategies for stocks/futures
3. **Performance Metrics** - Track execution quality
4. **Cost Analysis** - Verify data costs remain $0/month
5. **Live Trading** - Gradual rollout with small positions

---

**Status:** Ready to implement
**Estimated Completion:** 1-2 days
**Total Time:** 8-12 hours
