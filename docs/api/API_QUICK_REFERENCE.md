# AlgoTrendy API - Quick Reference Guide

**Simple, categorized reference for all API endpoints and WebSocket events**

---

## 🌐 Base URLs

| Environment | URL |
|-------------|-----|
| **Development** | `http://localhost:5298/api` |
| **Production** | `https://api.algotrendy.com/api` |
| **WebSocket** | `https://api.algotrendy.com/hubs/marketdata` |

---

## 📊 Market Data

**Get price and volume data for cryptocurrencies, stocks, and futures**

### Get Current Price

```
GET /api/v1/marketdata/{symbol}/latest
```

**Example:** Get latest Bitcoin price
```
GET /api/v1/marketdata/BTCUSDT/latest
```

**Returns:**
```json
{
  "symbol": "BTCUSDT",
  "close": 50000.00,
  "volume": 12345.67,
  "timestamp": "2025-10-20T12:00:00Z"
}
```

---

### Get Historical Data

```
GET /api/v1/marketdata/{symbol}?startTime={start}&endTime={end}
```

**Example:** Get BTC data for the last 24 hours
```
GET /api/v1/marketdata/BTCUSDT?startTime=2025-10-19T00:00:00Z&endTime=2025-10-20T00:00:00Z
```

**Returns:** Array of candles (OHLCV data)

---

### Get Multiple Symbols at Once

```
GET /api/v1/marketdata/latest?symbols={symbol1,symbol2,symbol3}
```

**Example:** Get BTC, ETH, and SOL prices
```
GET /api/v1/marketdata/latest?symbols=BTCUSDT,ETHUSDT,SOLUSDT
```

**Returns:** Object with prices for each symbol

---

### Get Aggregated Data (Hourly/Daily/Weekly)

```
GET /api/v1/marketdata/{symbol}/aggregated?interval={1h|1d|1w}
```

**Example:** Get daily BTC candles
```
GET /api/v1/marketdata/BTCUSDT/aggregated?interval=1d&startTime=...&endTime=...
```

---

### Stock Options Data

```
# Get available expiration dates
GET /api/v1/marketdata/stocks/AAPL/options/expirations

# Get options chain
GET /api/v1/marketdata/stocks/AAPL/options/chain?expiration=2025-12-20
```

---

### Futures Data

```
# Get open interest
GET /api/v1/marketdata/futures/BTCUSDT/openinterest

# Get funding rate
GET /api/v1/marketdata/futures/BTCUSDT/fundingrate
```

---

## 💰 Trading

**Place, cancel, and manage orders**

### Place an Order

```
POST /api/trading/orders
```

**Request Body:**
```json
{
  "symbol": "BTCUSDT",
  "exchange": "binance",
  "side": "Buy",
  "type": "Limit",
  "quantity": 0.01,
  "price": 50000
}
```

**Order Types:**
- `Market` - Execute immediately at current price
- `Limit` - Execute at specific price or better
- `StopLoss` - Trigger when price reaches stop level
- `StopLimit` - Limit order that triggers at stop price
- `TakeProfit` - Close position at profit target

**Order Sides:**
- `Buy` - Go long
- `Sell` - Go short or close long position

---

### Check Order Status

```
GET /api/trading/orders/{orderId}
```

**Order Statuses:**
- `Pending` - Created but not submitted
- `Open` - Active on exchange
- `PartiallyFilled` - Partially executed
- `Filled` - Completely filled
- `Cancelled` - Cancelled
- `Rejected` - Rejected by exchange
- `Expired` - Time limit reached

---

### Cancel an Order

```
DELETE /api/trading/orders/{orderId}
```

---

### Validate Order Before Placing

```
POST /api/trading/orders/validate
```

**Use this to check if an order will pass validation without actually placing it**

---

### Check Account Balance

```
GET /api/trading/balance/{exchange}/{currency}
```

**Example:** Check USDT balance on Binance
```
GET /api/trading/balance/binance/USDT
```

**Returns:**
```json
{
  "exchange": "binance",
  "currency": "USDT",
  "balance": 10000.50,
  "timestamp": "2025-10-20T12:00:00Z"
}
```

---

## 📈 Portfolio & Positions

**Monitor margin, leverage, and liquidation risk**

### Get Portfolio Summary

```
GET /api/portfolio/debt-summary
```

**Returns:**
```json
{
  "totalBorrowedAmount": 5000.00,
  "totalCollateralAmount": 10000.00,
  "marginHealthRatio": 0.25,
  "activeLeveragedPositions": 3,
  "isAtLiquidationRisk": false
}
```

---

### Get All Orders

```
GET /api/orders
```

**Returns:** List of all your orders

---

### Margin & Leverage

#### Check Margin Health
```
GET /api/portfolio/margin-health
```

**Returns:**
```json
{
  "marginHealthRatio": 0.25,
  "status": "HEALTHY",
  "isLiquidationRisk": false
}
```

**Health Status:**
- `HEALTHY` - Safe (> 50%)
- `CAUTION` - Monitor (15-50%)
- `WARNING` - Risk zone (5-15%)
- `CRITICAL` - Immediate liquidation risk (< 5%)

---

#### Get Leverage Info for Symbol
```
GET /api/portfolio/leverage/{symbol}
```

**Example:**
```
GET /api/portfolio/leverage/BTCUSDT
```

---

#### Set Leverage
```
PUT /api/portfolio/leverage
```

**Request:**
```json
{
  "symbol": "BTCUSDT",
  "leverage": 3.0,
  "marginType": "Isolated"
}
```

**Margin Types:**
- `Cross` - Entire account as collateral (shared risk)
- `Isolated` - Specific amount per position (isolated risk)

---

#### Check Liquidation Risk
```
GET /api/portfolio/liquidation-risk-positions
```

**Returns:** List of positions at risk of liquidation

---

#### Get Total Leverage Exposure
```
GET /api/portfolio/leverage-exposure
```

**Returns:** Total exposure across all leveraged positions

---

#### Close a Position
```
DELETE /api/portfolio/positions/{positionId}
```

**Request:**
```json
{
  "reason": "Manual close"
}
```

---

## 🔄 Backtesting

**Test trading strategies on historical data**

### Get Configuration Options

```
GET /api/v1/backtesting/config
```

**Returns:** Available symbols, timeframes, and indicators

---

### Run a Backtest

```
POST /api/v1/backtesting/run
```

**Request:**
```json
{
  "symbol": "BTCUSDT",
  "assetClass": "Cryptocurrency",
  "startDate": "2025-01-01",
  "endDate": "2025-10-01",
  "strategy": "momentum",
  "initialCapital": 10000
}
```

**Returns:** Full backtest results with metrics

---

### Get Backtest Results

```
GET /api/v1/backtesting/results/{id}
```

---

### Get Backtest History

```
GET /api/v1/backtesting/history?limit=50
```

**Returns:** List of your recent backtests

---

### Get Available Indicators

```
GET /api/v1/backtesting/indicators
```

**Returns:** List of technical indicators (RSI, MACD, Bollinger Bands, etc.)

---

### Delete a Backtest

```
DELETE /api/v1/backtesting/{id}
```

---

## 🪙 Cryptocurrency Data (Finnhub)

**Additional crypto market data from Finnhub**

### Get Crypto Candles

```
GET /api/v1/cryptodata/candles?symbol={exchange:symbol}&resolution={timeframe}
```

**Example:**
```
GET /api/v1/cryptodata/candles?symbol=BINANCE:BTCUSDT&resolution=60
```

**Resolutions:** `1` (1min), `5`, `15`, `30`, `60` (1hr), `D` (daily), `W` (weekly), `M` (monthly)

---

### Get Latest Quote

```
GET /api/v1/cryptodata/quote?symbol=BINANCE:BTCUSDT
```

---

### Get Supported Exchanges

```
GET /api/v1/cryptodata/exchanges
```

**Returns:** List of crypto exchanges (Binance, Coinbase, Kraken, etc.)

---

### Get Symbols for Exchange

```
GET /api/v1/cryptodata/symbols?exchange=binance
```

**Returns:** All trading pairs on that exchange

---

### Get Multiple Symbols (Batch)

```
GET /api/v1/cryptodata/candles/batch?symbols=BINANCE:BTCUSDT,BINANCE:ETHUSDT
```

**Note:** Max 10 symbols per request

---

## 📡 Real-Time Data (WebSocket)

**Live market data updates via SignalR**

### Connect to WebSocket

```javascript
import { HubConnectionBuilder } from '@microsoft/signalr';

const connection = new HubConnectionBuilder()
  .withUrl("https://api.algotrendy.com/hubs/marketdata")
  .build();

await connection.start();
```

---

### Subscribe to Symbols

```javascript
// Subscribe to BTC, ETH, SOL
await connection.invoke("SubscribeToSymbols", "BTCUSDT,ETHUSDT,SOLUSDT");
```

---

### Receive Market Data Updates

```javascript
connection.on("ReceiveMarketData", (data) => {
  console.log(`${data.symbol}: $${data.close}`);
  console.log(`Volume: ${data.volume}`);
});
```

**Data received:**
```json
{
  "symbol": "BTCUSDT",
  "timestamp": "2025-10-20T12:00:00Z",
  "open": 49500.00,
  "high": 50200.00,
  "low": 49300.00,
  "close": 50000.00,
  "volume": 12345.67
}
```

---

### Unsubscribe from Symbols

```javascript
await connection.invoke("UnsubscribeFromSymbols", "BTCUSDT");
```

---

### Test Connection (Ping)

```javascript
// Send ping
await connection.invoke("Ping");

// Receive pong
connection.on("Pong", (timestamp) => {
  console.log("Server time:", timestamp);
});
```

---

## 📦 Common Data Structures

### Market Data

```typescript
{
  symbol: string;          // "BTCUSDT"
  timestamp: DateTime;     // "2025-10-20T12:00:00Z"
  open: number;           // Opening price
  high: number;           // Highest price
  low: number;            // Lowest price
  close: number;          // Closing price
  volume: number;         // Trading volume
  source: string;         // "binance", "yfinance", etc.
}
```

---

### Order

```typescript
{
  orderId: string;           // "123e4567-e89b-12d3-a456-426614174000"
  clientOrderId: string;     // "AT_1234567890_abc123"
  exchangeOrderId: string;   // "binance-order-456"
  symbol: string;            // "BTCUSDT"
  exchange: string;          // "binance"
  side: "Buy" | "Sell";
  type: "Market" | "Limit" | "StopLoss" | "StopLimit" | "TakeProfit";
  status: "Pending" | "Open" | "PartiallyFilled" | "Filled" | "Cancelled" | "Rejected" | "Expired";
  quantity: number;          // 0.01
  filledQuantity: number;    // 0.005
  price: number;             // 50000 (null for market orders)
  averageFillPrice: number;  // 49950
  createdAt: DateTime;
  updatedAt: DateTime;
}
```

---

### Position

```typescript
{
  positionId: string;
  symbol: string;
  exchange: string;
  side: "Buy" | "Sell";      // Buy = Long, Sell = Short
  quantity: number;
  entryPrice: number;
  currentPrice: number;
  stopLoss: number;
  takeProfit: number;
  leverage: number;           // 1.0 = no leverage, 3.0 = 3x leverage
  marginType: "Cross" | "Isolated";
  liquidationPrice: number;
  marginHealthRatio: number;  // 0.0 to 1.0
  unrealizedPnL: number;      // Profit/loss in dollars
  unrealizedPnLPercent: number;
  openedAt: DateTime;
}
```

---

## ⚡ Quick Examples

### Example 1: Get Bitcoin Price

```bash
curl https://api.algotrendy.com/api/v1/marketdata/BTCUSDT/latest
```

---

### Example 2: Place Market Buy Order

```bash
curl -X POST https://api.algotrendy.com/api/trading/orders \
  -H "Content-Type: application/json" \
  -d '{
    "symbol": "BTCUSDT",
    "exchange": "binance",
    "side": "Buy",
    "type": "Market",
    "quantity": 0.01
  }'
```

---

### Example 3: Check Margin Health

```bash
curl https://api.algotrendy.com/api/portfolio/margin-health
```

---

### Example 4: Subscribe to Real-Time Prices

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

---

## 🛡️ Important Information

### Rate Limits

| Type | Limit |
|------|-------|
| Market Data | 1200 requests/min |
| Trading | 60 requests/min |
| Batch Operations | 100 requests/min |
| General | 100 requests/min |

---

### HTTP Status Codes

| Code | Meaning |
|------|---------|
| 200 | Success |
| 201 | Created |
| 204 | Deleted |
| 400 | Bad Request (check your data) |
| 404 | Not Found |
| 429 | Rate Limit Exceeded |
| 500 | Server Error |

---

### Asset Types

| Type | Examples | Exchanges |
|------|----------|-----------|
| **Cryptocurrency** | BTC, ETH, SOL | Binance, Bybit, Coinbase, Kraken |
| **Stock** | AAPL, GOOGL, MSFT | TradeStation, Interactive Brokers |
| **Futures** | ES, NQ, BTC perpetuals | NinjaTrader, Interactive Brokers |
| **Options** | Calls, Puts | Interactive Brokers, TradeStation |
| **ETF** | SPY, QQQ, IWM | TradeStation, Interactive Brokers |
| **Forex** | EUR/USD, GBP/USD | Interactive Brokers |

---

### Supported Exchanges

**Crypto:**
- Binance (crypto spot & futures)
- Bybit (crypto futures)
- Coinbase (crypto spot)
- Kraken (crypto spot)
- OKX (crypto futures)

**Stocks/Futures/Options:**
- Interactive Brokers (stocks, options, futures, forex)
- TradeStation (stocks, options, futures)
- NinjaTrader (futures)

---

## 💡 Tips

1. **Development vs Production**
   - Use `localhost:5298` for testing
   - Use `api.algotrendy.com` for production

2. **WebSocket Best Practices**
   - Subscribe only to symbols you need
   - Unsubscribe when done to save resources
   - Use ping/pong for keepalive

3. **Order Idempotency**
   - Use `clientOrderId` to prevent duplicate orders on network retries
   - If not provided, system auto-generates one

4. **Margin Safety**
   - Monitor `marginHealthRatio` regularly
   - Below 0.15 = warning zone
   - Below 0.05 = liquidation risk

5. **Rate Limits**
   - Batch requests when possible
   - Use WebSocket for real-time data instead of polling
   - Cache data on frontend when appropriate

---

## 📚 More Documentation

- **Full API Reference:** `docs/API_FRONTEND_BACKEND_INTERFACE.md`
- **JSON Schema:** `docs/API_FRONTEND_BACKEND_INTERFACE.json`
- **Domain Setup:** `docs/DOMAIN_DEPLOYMENT_GUIDE.md`

---

**Last Updated:** 2025-10-20
**Version:** AlgoTrendy v2.6
**For Questions:** See full documentation in `/docs` folder
