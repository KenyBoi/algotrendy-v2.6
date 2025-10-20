# AlgoTrendy v2.6 - Frontend-Backend Interface Documentation

**Complete library of all frontend-backend interface points**

This document provides a comprehensive mapping of all API endpoints, WebSocket events, and data models that the frontend uses to communicate with the backend.

---

## Table of Contents

1. [Base URL Configuration](#base-url-configuration)
2. [REST API Endpoints](#rest-api-endpoints)
   - [Market Data Controller](#market-data-controller)
   - [Trading Controller](#trading-controller)
   - [Orders Controller](#orders-controller)
   - [Portfolio Controller](#portfolio-controller)
   - [Backtesting Controller](#backtesting-controller)
   - [Crypto Data Controller](#crypto-data-controller)
3. [WebSocket (SignalR) Events](#websocket-signalr-events)
4. [Data Models](#data-models)
5. [Enumerations](#enumerations)

---

## Base URL Configuration

### Development
```
http://localhost:5298/api
```

### Production
```
https://api.algotrendy.com/api
```

### WebSocket (SignalR) Hub
- Development: `http://localhost:5298/hubs/marketdata`
- Production: `https://api.algotrendy.com/hubs/marketdata`

---

## REST API Endpoints

### Market Data Controller

**Base Route:** `/api/v1/marketdata`

#### 1. Get Market Data by Symbol

| Property | Value |
|----------|-------|
| **Route** | `GET /api/v1/marketdata/{symbol}` |
| **Description** | Get OHLCV market data for a specific symbol within a time range |
| **Type** | Request/Response |

**Query Parameters:**
- `startTime` (DateTime, required): Start time in UTC
- `endTime` (DateTime, required): End time in UTC

**Response:** `MarketData[]`

**Example:**
```
GET /api/v1/marketdata/BTCUSDT?startTime=2025-01-01T00:00:00Z&endTime=2025-01-02T00:00:00Z
```

---

#### 2. Get Latest Market Data

| Property | Value |
|----------|-------|
| **Route** | `GET /api/v1/marketdata/{symbol}/latest` |
| **Description** | Get the most recent market data for a symbol |
| **Type** | Request/Response |

**Response:** `MarketData`

---

#### 3. Get Latest Market Data (Batch)

| Property | Value |
|----------|-------|
| **Route** | `GET /api/v1/marketdata/latest` |
| **Description** | Get latest market data for multiple symbols at once |
| **Type** | Request/Response |

**Query Parameters:**
- `symbols` (string, required): Comma-separated list of symbols

**Response:** `Record<string, MarketData>`

**Example:**
```
GET /api/v1/marketdata/latest?symbols=BTCUSDT,ETHUSDT,SOLUSDT
```

---

#### 4. Get Aggregated Market Data

| Property | Value |
|----------|-------|
| **Route** | `GET /api/v1/marketdata/{symbol}/aggregated` |
| **Description** | Get aggregated candles (hourly, daily, weekly) |
| **Type** | Request/Response |

**Query Parameters:**
- `interval` (string, required): One of: "1h", "1d", "1w"
- `startTime` (DateTime, required): Start time in UTC
- `endTime` (DateTime, required): End time in UTC

**Response:** `MarketData[]`

---

#### 5. Insert Market Data

| Property | Value |
|----------|-------|
| **Route** | `POST /api/v1/marketdata` |
| **Description** | Insert a single market data record (admin only) |
| **Type** | Request/Response |

**Request Body:** `MarketData`

**Response:** `201 Created`

---

#### 6. Insert Market Data (Batch)

| Property | Value |
|----------|-------|
| **Route** | `POST /api/v1/marketdata/batch` |
| **Description** | Insert multiple market data records at once |
| **Type** | Request/Response |

**Request Body:** `MarketData[]`

**Response:** `number` (count of records inserted)

---

#### 7. Get Options Expirations (Stocks)

| Property | Value |
|----------|-------|
| **Route** | `GET /api/v1/marketdata/stocks/{symbol}/options/expirations` |
| **Description** | Get available option expiration dates for a stock |
| **Type** | Request/Response |

**Response:** `string[]` (dates in YYYY-MM-DD format)

**Example:**
```
GET /api/v1/marketdata/stocks/AAPL/options/expirations
```

---

#### 8. Get Options Chain (Stocks)

| Property | Value |
|----------|-------|
| **Route** | `GET /api/v1/marketdata/stocks/{symbol}/options/chain` |
| **Description** | Get options chain (calls and puts) for a stock and expiration |
| **Type** | Request/Response |

**Query Parameters:**
- `expiration` (string, required): Expiration date (YYYY-MM-DD)

**Response:** `object` (options chain data)

---

#### 9. Get Open Interest (Futures)

| Property | Value |
|----------|-------|
| **Route** | `GET /api/v1/marketdata/futures/{symbol}/openinterest` |
| **Description** | Get current open interest for a futures contract |
| **Type** | Request/Response |

**Response:** `number` (decimal)

---

#### 10. Get Funding Rate (Futures)

| Property | Value |
|----------|-------|
| **Route** | `GET /api/v1/marketdata/futures/{symbol}/fundingrate` |
| **Description** | Get current funding rate for perpetual futures |
| **Type** | Request/Response |

**Response:** `number` (decimal)

---

#### 11. Get Channel Status

| Property | Value |
|----------|-------|
| **Route** | `GET /api/v1/marketdata/channels/status` |
| **Description** | Get status of all market data channels |
| **Type** | Request/Response |

**Response:**
```typescript
{
  timestamp: DateTime,
  channels: Array<{
    name: string,
    available: boolean,
    connected: boolean,
    subscribedSymbols: number,
    lastDataReceived: DateTime | null,
    totalMessages: number
  }>
}
```

---

### Trading Controller

**Base Route:** `/api/trading`

#### 1. Place Order

| Property | Value |
|----------|-------|
| **Route** | `POST /api/trading/orders` |
| **Description** | Submit a new trading order |
| **Type** | Request/Response |

**Request Body:** `OrderRequest`

**Response:** `Order` (with ExchangeOrderId populated)

**Example:**
```json
{
  "symbol": "BTCUSDT",
  "exchange": "binance",
  "side": "Buy",
  "type": "Limit",
  "quantity": 0.01,
  "price": 50000,
  "clientOrderId": "AT_1234567890_abc123"
}
```

---

#### 2. Cancel Order

| Property | Value |
|----------|-------|
| **Route** | `DELETE /api/trading/orders/{orderId}` |
| **Description** | Cancel an active order |
| **Type** | Request/Response |

**Response:** `Order` (with status "Cancelled")

---

#### 3. Get Order Status

| Property | Value |
|----------|-------|
| **Route** | `GET /api/trading/orders/{orderId}` |
| **Description** | Get current status of a specific order |
| **Type** | Request/Response |

**Response:** `Order`

---

#### 4. Validate Order

| Property | Value |
|----------|-------|
| **Route** | `POST /api/trading/orders/validate` |
| **Description** | Validate an order before submission (no actual order placed) |
| **Type** | Request/Response |

**Request Body:** `OrderRequest`

**Response:** `ValidationResult`

```typescript
{
  isValid: boolean,
  message: string
}
```

---

#### 5. Get Balance

| Property | Value |
|----------|-------|
| **Route** | `GET /api/trading/balance/{exchange}/{currency}` |
| **Description** | Get account balance for a specific currency on an exchange |
| **Type** | Request/Response |

**Response:** `BalanceResponse`

```typescript
{
  exchange: string,
  currency: string,
  balance: number,
  timestamp: DateTime
}
```

**Example:**
```
GET /api/trading/balance/binance/USDT
```

---

### Orders Controller

**Base Route:** `/api/orders`

#### 1. Get All Orders

| Property | Value |
|----------|-------|
| **Route** | `GET /api/orders` |
| **Description** | Retrieve all orders |
| **Type** | Request/Response |

**Response:** `Order[]`

---

### Portfolio Controller

**Base Route:** `/api/portfolio`

#### 1. Get Debt Summary

| Property | Value |
|----------|-------|
| **Route** | `GET /api/portfolio/debt-summary` |
| **Description** | Get comprehensive debt and margin summary |
| **Type** | Request/Response |

**Response:** `DebtSummary`

---

#### 2. Get Leverage Info

| Property | Value |
|----------|-------|
| **Route** | `GET /api/portfolio/leverage/{symbol}` |
| **Description** | Get leverage information for a specific symbol |
| **Type** | Request/Response |

**Response:** `LeverageInfo`

---

#### 3. Set Leverage

| Property | Value |
|----------|-------|
| **Route** | `PUT /api/portfolio/leverage` |
| **Description** | Set leverage for a trading symbol |
| **Type** | Request/Response |

**Request Body:** `SetLeverageRequest`

```typescript
{
  symbol: string,
  leverage: number,
  marginType: "Cross" | "Isolated"
}
```

**Response:** `LeverageInfo`

---

#### 4. Get Margin Health

| Property | Value |
|----------|-------|
| **Route** | `GET /api/portfolio/margin-health` |
| **Description** | Get account-wide margin health ratio |
| **Type** | Request/Response |

**Response:** `MarginHealthResponse`

```typescript
{
  marginHealthRatio: number,
  status: "HEALTHY" | "CAUTION" | "WARNING" | "CRITICAL",
  isLiquidationRisk: boolean,
  timestamp: DateTime
}
```

---

#### 5. Get Leverage Exposure

| Property | Value |
|----------|-------|
| **Route** | `GET /api/portfolio/leverage-exposure` |
| **Description** | Get total leverage exposure across all positions |
| **Type** | Request/Response |

**Response:** `LeverageExposureResponse`

```typescript
{
  totalLeverageExposure: number,
  timestamp: DateTime
}
```

---

#### 6. Get Liquidation Risk Positions

| Property | Value |
|----------|-------|
| **Route** | `GET /api/portfolio/liquidation-risk-positions` |
| **Description** | Get all positions at risk of liquidation |
| **Type** | Request/Response |

**Response:** `Position[]`

---

#### 7. Validate Leverage

| Property | Value |
|----------|-------|
| **Route** | `POST /api/portfolio/validate-leverage` |
| **Description** | Validate if a leverage setting is allowed |
| **Type** | Request/Response |

**Request Body:** `ValidateLeverageRequest`

```typescript
{
  symbol: string,
  proposedLeverage: number
}
```

**Response:** `LeverageValidationResponse`

```typescript
{
  isValid: boolean,
  symbol: string,
  proposedLeverage: number,
  message: string
}
```

---

#### 8. Get Margin Configuration

| Property | Value |
|----------|-------|
| **Route** | `GET /api/portfolio/margin-configuration` |
| **Description** | Get broker's margin configuration limits |
| **Type** | Request/Response |

**Response:** `MarginConfiguration`

---

#### 9. Close Position

| Property | Value |
|----------|-------|
| **Route** | `DELETE /api/portfolio/positions/{positionId}` |
| **Description** | Close a leveraged position and settle debt |
| **Type** | Request/Response |

**Request Body:** `ClosePositionRequest`

```typescript
{
  reason: string
}
```

**Response:** `ClosePositionResponse`

```typescript
{
  success: boolean,
  positionId: string,
  reason: string,
  closedAt: DateTime,
  message: string
}
```

---

### Backtesting Controller

**Base Route:** `/api/v1/backtesting`

#### 1. Get Configuration Options

| Property | Value |
|----------|-------|
| **Route** | `GET /api/v1/backtesting/config` |
| **Description** | Get available configuration options for backtesting UI |
| **Type** | Request/Response |

**Response:** `BacktestConfigOptions`

---

#### 2. Run Backtest

| Property | Value |
|----------|-------|
| **Route** | `POST /api/v1/backtesting/run` |
| **Description** | Execute a backtest with the given configuration |
| **Type** | Request/Response |

**Request Body:** `BacktestConfig`

**Response:** `BacktestResults`

---

#### 3. Get Backtest Results

| Property | Value |
|----------|-------|
| **Route** | `GET /api/v1/backtesting/results/{id}` |
| **Description** | Get results of a previous backtest |
| **Type** | Request/Response |

**Response:** `BacktestResults`

---

#### 4. Get Backtest History

| Property | Value |
|----------|-------|
| **Route** | `GET /api/v1/backtesting/history` |
| **Description** | Get list of recent backtests |
| **Type** | Request/Response |

**Query Parameters:**
- `limit` (number, optional): Max results (default: 50, max: 500)

**Response:** `BacktestHistoryItem[]`

---

#### 5. Get Available Indicators

| Property | Value |
|----------|-------|
| **Route** | `GET /api/v1/backtesting/indicators` |
| **Description** | Get list of available technical indicators |
| **Type** | Request/Response |

**Response:** `Record<string, Record<string, any>>`

---

#### 6. Delete Backtest

| Property | Value |
|----------|-------|
| **Route** | `DELETE /api/v1/backtesting/{id}` |
| **Description** | Delete a backtest by ID |
| **Type** | Request/Response |

**Response:** `204 No Content`

---

### Crypto Data Controller

**Base Route:** `/api/v1/cryptodata`

#### 1. Get Crypto Candles

| Property | Value |
|----------|-------|
| **Route** | `GET /api/v1/cryptodata/candles` |
| **Description** | Get cryptocurrency OHLCV candlestick data |
| **Type** | Request/Response |

**Query Parameters:**
- `symbol` (string, required): Trading symbol (e.g., "BINANCE:BTCUSDT")
- `resolution` (string, optional): "1", "5", "15", "30", "60", "D", "W", "M" (default: "1")
- `from` (number, optional): Unix timestamp (seconds)
- `to` (number, optional): Unix timestamp (seconds)

**Response:** `CryptoCandle[]`

---

#### 2. Get Crypto Quote

| Property | Value |
|----------|-------|
| **Route** | `GET /api/v1/cryptodata/quote` |
| **Description** | Get latest price for a cryptocurrency |
| **Type** | Request/Response |

**Query Parameters:**
- `symbol` (string, required): Trading symbol (e.g., "BINANCE:BTCUSDT")

**Response:** `QuoteResponse`

```typescript
{
  symbol: string,
  price: number,
  timestamp: DateTime
}
```

---

#### 3. Get Crypto Exchanges

| Property | Value |
|----------|-------|
| **Route** | `GET /api/v1/cryptodata/exchanges` |
| **Description** | Get list of supported cryptocurrency exchanges |
| **Type** | Request/Response |

**Response:** `CryptoExchange[]`

---

#### 4. Get Crypto Symbols

| Property | Value |
|----------|-------|
| **Route** | `GET /api/v1/cryptodata/symbols` |
| **Description** | Get list of symbols for a specific exchange |
| **Type** | Request/Response |

**Query Parameters:**
- `exchange` (string, required): Exchange code (e.g., "binance", "coinbase")

**Response:** `CryptoSymbol[]`

---

#### 5. Get Crypto Candles (Batch)

| Property | Value |
|----------|-------|
| **Route** | `GET /api/v1/cryptodata/candles/batch` |
| **Description** | Get candles for multiple symbols (max 10) |
| **Type** | Request/Response |

**Query Parameters:**
- `symbols` (string, required): Comma-separated list (max 10)
- `resolution` (string, optional): Candle resolution
- `from` (number, optional): Unix timestamp
- `to` (number, optional): Unix timestamp

**Response:** `Record<string, CryptoCandle[]>`

---

## WebSocket (SignalR) Events

### Hub Connection

**Hub URL:** `/hubs/marketdata`

**Protocol:** SignalR (WebSocket with fallback)

### Client → Server Methods

#### 1. Subscribe to Symbols

| Property | Value |
|----------|-------|
| **Method** | `SubscribeToSymbols` |
| **Description** | Subscribe to real-time market data for specific symbols |
| **Type** | Client → Server |

**Parameters:**
- `symbols` (string): Comma-separated list of symbols (e.g., "BTCUSDT,ETHUSDT")

**Server Response Event:** `Subscribed`

**Example:**
```javascript
connection.invoke("SubscribeToSymbols", "BTCUSDT,ETHUSDT,SOLUSDT");
```

---

#### 2. Unsubscribe from Symbols

| Property | Value |
|----------|-------|
| **Method** | `UnsubscribeFromSymbols` |
| **Description** | Unsubscribe from market data updates |
| **Type** | Client → Server |

**Parameters:**
- `symbols` (string): Comma-separated list of symbols

**Server Response Event:** `Unsubscribed`

---

#### 3. Ping

| Property | Value |
|----------|-------|
| **Method** | `Ping` |
| **Description** | Test connection (keepalive) |
| **Type** | Client → Server |

**Server Response Event:** `Pong`

**Response Data:** `DateTime` (server timestamp)

---

### Server → Client Events

#### 1. Market Data Update

| Property | Value |
|----------|-------|
| **Event** | `ReceiveMarketData` |
| **Description** | Real-time market data updates |
| **Type** | Server → Client (Real-time) |

**Data:** `MarketData`

**Example:**
```javascript
connection.on("ReceiveMarketData", (marketData) => {
  console.log(marketData.symbol, marketData.close, marketData.volume);
});
```

---

#### 2. Subscribed Confirmation

| Property | Value |
|----------|-------|
| **Event** | `Subscribed` |
| **Description** | Confirmation of successful subscription |
| **Type** | Server → Client |

**Data:** `string[]` (list of subscribed symbols)

---

#### 3. Unsubscribed Confirmation

| Property | Value |
|----------|-------|
| **Event** | `Unsubscribed` |
| **Description** | Confirmation of successful unsubscription |
| **Type** | Server → Client |

**Data:** `string[]` (list of unsubscribed symbols)

---

#### 4. Pong Response

| Property | Value |
|----------|-------|
| **Event** | `Pong` |
| **Description** | Response to ping (connection test) |
| **Type** | Server → Client |

**Data:** `DateTime` (server timestamp)

---

## Data Models

### MarketData

```typescript
interface MarketData {
  symbol: string;              // Trading symbol (e.g., "BTCUSDT")
  timestamp: DateTime;         // Candle timestamp (UTC)
  open: number;               // Opening price
  high: number;               // Highest price
  low: number;                // Lowest price
  close: number;              // Closing price
  volume: number;             // Volume in base currency
  quoteVolume?: number;       // Volume in quote currency
  tradesCount?: number;       // Number of trades
  source: string;             // Data source (e.g., "binance", "yfinance")
  assetType: AssetType;       // Asset classification
  metadata?: string;          // Additional metadata (JSON)

  // Computed properties
  changePercent: number;      // Price change percentage
  range: number;              // High - Low
  isBullish: boolean;         // Close > Open
  isBearish: boolean;         // Close < Open
}
```

---

### Order

```typescript
interface Order {
  orderId: string;            // Unique order ID (UUID)
  clientOrderId: string;      // Client idempotency key
  exchangeOrderId?: string;   // Exchange-provided ID
  symbol: string;             // Trading symbol
  exchange: string;           // Exchange name
  side: OrderSide;            // "Buy" | "Sell"
  type: OrderType;            // Order type
  status: OrderStatus;        // Current status
  quantity: number;           // Order quantity
  filledQuantity: number;     // Filled quantity
  price?: number;             // Limit price (null for market)
  stopPrice?: number;         // Stop price
  averageFillPrice?: number;  // Average execution price
  strategyId?: string;        // Strategy that created order
  createdAt: DateTime;        // Creation timestamp (UTC)
  updatedAt: DateTime;        // Last update timestamp (UTC)
  submittedAt?: DateTime;     // Exchange submission time
  closedAt?: DateTime;        // Completion time
  metadata?: string;          // Additional metadata (JSON)

  // Computed properties
  remainingQuantity: number;  // Unfilled quantity
  isTerminal: boolean;        // Cannot be modified
  isActive: boolean;          // Active on exchange
}
```

---

### OrderRequest

```typescript
interface OrderRequest {
  clientOrderId?: string;     // Optional idempotency key (auto-generated if not provided)
  symbol: string;             // Trading symbol (required)
  exchange: string;           // Exchange name (required)
  side: OrderSide;            // "Buy" | "Sell" (required)
  type: OrderType;            // Order type (required)
  quantity: number;           // Quantity (required)
  price?: number;             // Limit price (required for Limit orders)
  stopPrice?: number;         // Stop price (required for Stop orders)
  strategyId?: string;        // Optional strategy ID
  metadata?: string;          // Optional metadata (JSON)
}
```

---

### Position

```typescript
interface Position {
  positionId: string;         // Unique position ID
  symbol: string;             // Trading symbol
  exchange: string;           // Exchange name
  side: OrderSide;            // "Buy" (long) | "Sell" (short)
  quantity: number;           // Position size
  entryPrice: number;         // Average entry price
  currentPrice: number;       // Current market price
  stopLoss?: number;          // Stop loss price
  takeProfit?: number;        // Take profit price
  strategyId?: string;        // Strategy that opened position
  openOrderId?: string;       // Order that opened position
  openedAt: DateTime;         // Open timestamp (UTC)
  updatedAt: DateTime;        // Last update timestamp (UTC)

  // Margin/Leverage fields
  leverage: number;           // Leverage multiplier (1.0 for spot)
  marginType?: MarginType;    // "Cross" | "Isolated"
  collateralAmount?: number;  // Collateral amount
  borrowedAmount?: number;    // Borrowed funds
  interestRate?: number;      // Interest rate (daily %)
  liquidationPrice?: number;  // Liquidation price
  marginHealthRatio?: number; // Margin health (0.0 to 1.0)

  // Computed properties
  unrealizedPnL: number;       // Profit/loss in dollars
  unrealizedPnLPercent: number; // Profit/loss percentage
  entryValue: number;          // Total entry value
  currentValue: number;        // Current total value
  isStopLossHit: boolean;      // Stop loss triggered
  isTakeProfitHit: boolean;    // Take profit triggered
  isMarginPosition: boolean;   // Using leverage
  effectivePositionSize: number; // Size with leverage
  usedMargin: number;          // Margin amount used
  isInLiquidationRisk: boolean; // At risk of liquidation
}
```

---

### DebtSummary

```typescript
interface DebtSummary {
  totalBorrowedAmount: number;       // Total borrowed funds
  totalCollateralAmount: number;     // Total collateral
  marginHealthRatio: number;         // Overall margin health
  activeLeveragedPositions: number;  // Count of leveraged positions
  totalLeverageExposure: number;     // Total exposure with leverage
  isAtLiquidationRisk: boolean;      // Account at risk
  timestamp: DateTime;               // Calculation timestamp
}
```

---

### LeverageInfo

```typescript
interface LeverageInfo {
  symbol: string;                    // Trading symbol
  currentLeverage: number;           // Current leverage setting
  marginType: MarginType;            // "Cross" | "Isolated"
  collateralAmount: number;          // Collateral posted
  borrowedAmount: number;            // Borrowed funds
  liquidationPrice?: number;         // Liquidation price
  marginHealthRatio: number;         // Position margin health
  isLiquidationRisk: boolean;        // Position at risk
}
```

---

### CryptoCandle

```typescript
interface CryptoCandle {
  timestamp: DateTime;       // Candle timestamp (UTC)
  open: number;             // Opening price
  high: number;             // Highest price
  low: number;              // Lowest price
  close: number;            // Closing price
  volume: number;           // Volume traded
}
```

---

## Enumerations

### OrderSide

```typescript
enum OrderSide {
  Buy = "Buy",      // Buy order - going long
  Sell = "Sell"     // Sell order - going short or closing long
}
```

---

### OrderType

```typescript
enum OrderType {
  Market = "Market",           // Executes at current market price
  Limit = "Limit",             // Executes at specified price or better
  StopLoss = "StopLoss",       // Triggers when price reaches stop
  StopLimit = "StopLimit",     // Becomes limit order at stop price
  TakeProfit = "TakeProfit"    // Closes position at profit target
}
```

---

### OrderStatus

```typescript
enum OrderStatus {
  Pending = "Pending",                 // Created but not submitted
  Open = "Open",                       // Active on exchange
  PartiallyFilled = "PartiallyFilled", // Partially filled
  Filled = "Filled",                   // Completely filled
  Cancelled = "Cancelled",             // Cancelled
  Rejected = "Rejected",               // Rejected by exchange
  Expired = "Expired"                  // Expired (time-in-force)
}
```

---

### AssetType

```typescript
enum AssetType {
  Cryptocurrency = 0,  // BTC, ETH, SOL, etc.
  Stock = 1,           // AAPL, GOOGL, MSFT, etc.
  Futures = 2,         // ES, NQ, BTC perpetuals, etc.
  Options = 3,         // Calls and puts
  ETF = 4,             // SPY, QQQ, IWM, etc.
  Forex = 5            // EUR/USD, GBP/USD, etc.
}
```

---

### MarginType

```typescript
enum MarginType {
  Cross = "Cross",         // Entire account as collateral
  Isolated = "Isolated"    // Specific amount per position
}
```

---

## HTTP Status Codes

| Code | Meaning |
|------|---------|
| 200 | Success |
| 201 | Created (resource created successfully) |
| 204 | No Content (successful deletion) |
| 400 | Bad Request (validation error) |
| 404 | Not Found (resource doesn't exist) |
| 429 | Too Many Requests (rate limit exceeded) |
| 500 | Internal Server Error |
| 503 | Service Unavailable (channel not available) |

---

## Error Response Format

All error responses follow this format:

```typescript
{
  error: string  // Human-readable error message
}
```

---

## Rate Limits

| Endpoint Type | Limit |
|--------------|-------|
| Market Data | 1200 requests/minute per IP |
| Trading Operations | 60 requests/minute per IP |
| Batch Operations | 100 requests/minute per IP |
| General | 100 requests/minute per IP |

---

## Authentication

**Current Status:** No authentication required (development phase)

**Future:** API key authentication via `X-API-Key` header

---

## CORS Configuration

The following origins are allowed:
- `http://localhost:3000` (Next.js dev)
- `http://localhost:5000` (API dev)
- `http://localhost:5298` (.NET dev)
- `https://localhost:7228` (.NET HTTPS dev)
- `https://algotrendy.com`
- `https://www.algotrendy.com`
- `https://app.algotrendy.com`
- `https://api.algotrendy.com`

---

## Example: Complete Trading Flow

### 1. Subscribe to Market Data (WebSocket)

```javascript
const connection = new HubConnectionBuilder()
  .withUrl("https://api.algotrendy.com/hubs/marketdata")
  .build();

await connection.start();

connection.on("ReceiveMarketData", (data) => {
  console.log(`${data.symbol}: $${data.close}`);
});

await connection.invoke("SubscribeToSymbols", "BTCUSDT,ETHUSDT");
```

### 2. Get Latest Market Data (REST)

```javascript
const response = await fetch(
  "https://api.algotrendy.com/api/v1/marketdata/BTCUSDT/latest"
);
const marketData = await response.json();
console.log(marketData);
```

### 3. Validate Order

```javascript
const orderRequest = {
  symbol: "BTCUSDT",
  exchange: "binance",
  side: "Buy",
  type: "Limit",
  quantity: 0.01,
  price: 50000
};

const validateResponse = await fetch(
  "https://api.algotrendy.com/api/trading/orders/validate",
  {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(orderRequest)
  }
);
const validation = await validateResponse.json();

if (validation.isValid) {
  // Proceed with order
}
```

### 4. Place Order

```javascript
const orderResponse = await fetch(
  "https://api.algotrendy.com/api/trading/orders",
  {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(orderRequest)
  }
);
const order = await orderResponse.json();
console.log("Order ID:", order.orderId);
console.log("Exchange Order ID:", order.exchangeOrderId);
```

### 5. Check Order Status

```javascript
const statusResponse = await fetch(
  `https://api.algotrendy.com/api/trading/orders/${order.orderId}`
);
const orderStatus = await statusResponse.json();
console.log("Status:", orderStatus.status);
console.log("Filled:", orderStatus.filledQuantity, "/", orderStatus.quantity);
```

### 6. Cancel Order (if needed)

```javascript
const cancelResponse = await fetch(
  `https://api.algotrendy.com/api/trading/orders/${order.orderId}`,
  { method: "DELETE" }
);
const cancelledOrder = await cancelResponse.json();
console.log("Cancelled:", cancelledOrder.status === "Cancelled");
```

### 7. Monitor Portfolio

```javascript
// Get margin health
const healthResponse = await fetch(
  "https://api.algotrendy.com/api/portfolio/margin-health"
);
const health = await healthResponse.json();
console.log("Margin Health:", health.marginHealthRatio);
console.log("Status:", health.status);

// Check for liquidation risk
const riskResponse = await fetch(
  "https://api.algotrendy.com/api/portfolio/liquidation-risk-positions"
);
const riskPositions = await riskResponse.json();
if (riskPositions.length > 0) {
  console.warn("Positions at risk:", riskPositions);
}
```

---

**Last Updated:** 2025-10-20
**Version:** AlgoTrendy v2.6
**Maintained by:** AlgoTrendy Development Team
