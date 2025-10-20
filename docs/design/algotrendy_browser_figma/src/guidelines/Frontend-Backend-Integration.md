# Frontend-Backend Integration Map

**Comprehensive mapping of React components to .NET backend endpoints**

---

## 🎯 Overview

This document maps each frontend component to its corresponding backend API endpoints, showing exactly how the React/TypeScript frontend connects to the .NET/C# backend.

---

## 📱 Component → Endpoint Mapping

### 1. **Market Data Dashboard** (To Be Created)

**Purpose:** Real-time and historical market data for crypto, stocks, futures

**Backend Endpoints:**
```
GET  /api/v1/marketdata/{symbol}/latest
GET  /api/v1/marketdata/{symbol}?startTime={start}&endTime={end}
GET  /api/v1/marketdata/latest?symbols={symbols}
GET  /api/v1/marketdata/{symbol}/aggregated?interval={interval}
```

**WebSocket:**
```
SignalR Hub: /hubs/marketdata
Events: SubscribeToSymbols, ReceiveMarketData, UnsubscribeFromSymbols
```

**Service File:** `/services/marketDataService.ts` (To Be Created)

**Components Needed:**
- `MarketDataDashboard.tsx` - Main dashboard
- `PriceChart.tsx` - Real-time price charts
- `MarketOverview.tsx` - Multi-symbol overview
- `TickerTape.tsx` - Scrolling ticker

---

### 2. **Strategy Builder** (Existing - Needs Update)

**Current:** `/components/StrategyBuilder.tsx`

**Purpose:** Build, test, and optimize trading strategies

**Backend Endpoints:**
```
GET  /api/v1/backtesting/config
POST /api/v1/backtesting/run
GET  /api/v1/backtesting/results/{id}
GET  /api/v1/backtesting/history?limit=50
GET  /api/v1/backtesting/indicators
DELETE /api/v1/backtesting/{id}
```

**Service File:** `/services/strategyService.ts` (Update Needed)

**Integration Tasks:**
- [ ] Update `strategyService.ts` to use `/api/v1/backtesting/*` endpoints
- [ ] Add backtest results visualization
- [ ] Add indicator configuration UI
- [ ] Add backtest history table
- [ ] Add strategy save/load functionality

---

### 3. **Trading Terminal** (To Be Created)

**Purpose:** Place, manage, and monitor orders

**Backend Endpoints:**
```
POST   /api/trading/orders
GET    /api/trading/orders/{orderId}
DELETE /api/trading/orders/{orderId}
POST   /api/trading/orders/validate
GET    /api/trading/balance/{exchange}/{currency}
GET    /api/orders
```

**Service File:** `/services/tradingService.ts` (To Be Created)

**Components Needed:**
- `TradingTerminal.tsx` - Main trading interface
- `OrderForm.tsx` - Place orders
- `OrderBook.tsx` - Live order book
- `OrderHistory.tsx` - Past orders
- `BalanceDisplay.tsx` - Account balances

---

### 4. **Portfolio Manager** (To Be Created)

**Purpose:** Monitor positions, margin, leverage, and risk

**Backend Endpoints:**
```
GET    /api/portfolio/debt-summary
GET    /api/portfolio/margin-health
GET    /api/portfolio/leverage/{symbol}
PUT    /api/portfolio/leverage
GET    /api/portfolio/liquidation-risk-positions
GET    /api/portfolio/leverage-exposure
DELETE /api/portfolio/positions/{positionId}
```

**Service File:** `/services/portfolioService.ts` (To Be Created)

**Components Needed:**
- `PortfolioOverview.tsx` - Portfolio summary
- `PositionsTable.tsx` - All positions
- `MarginHealthIndicator.tsx` - Visual margin health
- `RiskAlerts.tsx` - Liquidation warnings
- `LeverageControls.tsx` - Adjust leverage

---

### 5. **AI Assistant** (Existing - Connected)

**Current:** `/components/AIAssistant.tsx` ✅

**Purpose:** AI-powered trading insights and analysis

**Backend Endpoints:**
```
POST /api/ai/chat
POST /api/ai/analyze
GET  /api/ai/suggestions
```

**Service File:** `/services/aiService.ts` ✅

**Status:** Already integrated and working

---

### 6. **Query Builder** (Existing - Needs Update)

**Current:** `/components/QueryBuilder.tsx`

**Purpose:** Custom QuestDB queries for data analysis

**Backend Endpoints:**
```
POST /api/queries/execute
POST /api/queries/save
GET  /api/queries/history
```

**Plus Market Data for querying:**
```
GET /api/v1/marketdata/{symbol}?startTime={start}&endTime={end}
```

**Service File:** `/services/queryService.ts` (Update Needed)

**Integration Tasks:**
- [ ] Update to query market data endpoints
- [ ] Add query result visualization
- [ ] Add saved query management
- [ ] Add export functionality

---

### 7. **Dataset Browser** (Existing - Partial)

**Current:** `/components/DatasetBrowser.tsx`

**Purpose:** Browse company fundamentals and financial metrics

**Backend Endpoints:**
```
Currently using mock endpoints - needs to be mapped to actual data source
```

**Note:** This component may need to be repurposed for stock fundamentals data or integrated with market data endpoints for financial metrics.

---

## 🔌 Service Layer Architecture

### Current Services

```
/services/
├── api.ts              ✅ Generic API wrapper
├── aiService.ts        ✅ AI assistant
├── datasetService.ts   ⚠️  Needs updating
├── queryService.ts     ⚠️  Needs updating
├── strategyService.ts  ⚠️  Needs updating
└── index.ts            ✅ Exports all services
```

### Services To Create

```
/services/
├── marketDataService.ts    ❌ Market data & prices
├── tradingService.ts       ❌ Order management
├── portfolioService.ts     ❌ Positions & margin
├── cryptoDataService.ts    ❌ Finnhub crypto data
└── websocketService.ts     ❌ Real-time SignalR
```

---

## 📊 Data Flow Examples

### Example 1: Real-Time Price Updates

```
User opens Market Dashboard
    ↓
Component mounts → useEffect()
    ↓
websocketService.connect()
    ↓
SignalR connects to /hubs/marketdata
    ↓
Subscribe to symbols: "BTCUSDT,ETHUSDT,SOLUSDT"
    ↓
Receive ReceiveMarketData events
    ↓
Update React state → UI re-renders
```

### Example 2: Place a Trade

```
User fills out order form
    ↓
Click "Place Order"
    ↓
tradingService.validateOrder() [POST /api/trading/orders/validate]
    ↓
If valid → tradingService.placeOrder() [POST /api/trading/orders]
    ↓
Backend processes → Returns order ID
    ↓
Poll order status [GET /api/trading/orders/{id}]
    ↓
Update UI with order status
```

### Example 3: Run Backtest

```
User configures strategy in StrategyBuilder
    ↓
Click "Run Backtest"
    ↓
strategyService.runBacktest() [POST /api/v1/backtesting/run]
    ↓
Backend runs backtest (may take time)
    ↓
Poll for results [GET /api/v1/backtesting/results/{id}]
    ↓
Display results in charts and metrics
```

---

## 🎨 New Components Needed

### High Priority

1. **MarketDataDashboard** - Real-time prices and charts
2. **TradingTerminal** - Place and manage orders
3. **PortfolioOverview** - Monitor positions and risk
4. **RealTimeChart** - Live candlestick charts with WebSocket data

### Medium Priority

5. **OrderBook** - Live order book visualization
6. **PositionsTable** - All open positions with P&L
7. **BacktestResults** - Visualize backtest performance
8. **MarginHealthIndicator** - Risk warning system

### Low Priority

9. **ExchangeSelector** - Choose trading exchange
10. **AssetClassFilter** - Filter by crypto/stocks/futures
11. **TradingHistory** - Historical trades
12. **PerformanceMetrics** - Trading performance analytics

---

## 🔧 Implementation Priorities

### Phase 1: Core Trading (Week 1-2)
- [ ] Create `marketDataService.ts`
- [ ] Create `MarketDataDashboard` component
- [ ] Implement WebSocket connection (`websocketService.ts`)
- [ ] Real-time price display

### Phase 2: Trading Terminal (Week 3-4)
- [ ] Create `tradingService.ts`
- [ ] Create `TradingTerminal` component
- [ ] Order placement and validation
- [ ] Order status monitoring

### Phase 3: Portfolio & Risk (Week 5-6)
- [ ] Create `portfolioService.ts`
- [ ] Create `PortfolioOverview` component
- [ ] Margin health monitoring
- [ ] Risk alerts and warnings

### Phase 4: Strategy & Backtesting (Week 7-8)
- [ ] Update `strategyService.ts`
- [ ] Update `StrategyBuilder` component
- [ ] Backtest visualization
- [ ] Strategy optimization

---

## 📦 TypeScript Types Needed

### Current Types (in `/types/index.ts`)

```typescript
✅ ApiResponse
✅ Message (AI chat)
✅ ChatRequest
✅ AnalysisRequest
⚠️  Strategy (needs update)
⚠️  BacktestConfig (needs update)
```

### Types To Add

```typescript
❌ MarketData
❌ Order
❌ OrderRequest
❌ Position
❌ Portfolio
❌ MarginHealth
❌ Balance
❌ BacktestResult
❌ Indicator
❌ WebSocketMessage
```

**See `/guidelines/Backend-API-Reference.md` for complete type definitions**

---

## 🧪 Testing Strategy

### Unit Tests
- Service layer functions
- API request/response handling
- WebSocket connection management

### Integration Tests
- Component → Service → API flow
- WebSocket real-time updates
- Error handling and retries

### E2E Tests
- Complete user flows (place order, monitor position, etc.)
- Real backend integration
- Performance under load

---

## 🚀 Quick Start for Developers

### 1. Start Backend
```bash
cd backend
dotnet run
# Backend runs on http://localhost:5298
```

### 2. Start Frontend
```bash
cd frontend
npm run dev
# Frontend runs on http://localhost:3000
```

### 3. Test Connection
Open browser console and check:
```javascript
// Test API connection
fetch('http://localhost:5298/api/v1/marketdata/BTCUSDT/latest')
  .then(r => r.json())
  .then(console.log);
```

### 4. Enable Backend Mode
Toggle "Use .NET Backend" checkbox in the UI

---

## 📚 Related Documentation

- **Backend API Reference:** `/guidelines/Backend-API-Reference.md`
- **TypeScript Types:** `/types/index.ts`
- **Service Layer:** `/services/`
- **Components:** `/components/`

---

**Last Updated:** 2025-10-20  
**Status:** Phase 1 Ready - Awaiting Component Creation
