# AlgoTrendy API - Usage Examples

This guide provides practical examples for interacting with the AlgoTrendy API using different programming languages and tools.

## Table of Contents

- [Authentication](#authentication)
- [Market Data](#market-data)
- [Orders](#orders)
- [Backtesting](#backtesting)
- [Portfolio](#portfolio)
- [WebSocket (SignalR)](#websocket-signalr)

---

## Base URL

```
Development: http://localhost:5002/api
Production:  https://api.algotrendy.com/api
```

## Authentication

AlgoTrendy uses Bearer token authentication (JWT).

### cURL

```bash
# Login to get access token
curl -X POST http://localhost:5002/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "username": "your_username",
    "password": "your_password"
  }'

# Response:
# {
#   "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
#   "expiration": "2025-10-21T12:00:00Z"
# }

# Use token in subsequent requests
curl -X GET http://localhost:5002/api/portfolio/summary \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

### Python

```python
import requests

class AlgoTrendyClient:
    def __init__(self, base_url="http://localhost:5002/api"):
        self.base_url = base_url
        self.token = None

    def login(self, username, password):
        """Authenticate and store access token"""
        response = requests.post(
            f"{self.base_url}/auth/login",
            json={"username": username, "password": password}
        )
        response.raise_for_status()
        self.token = response.json()["token"]
        return self.token

    def _headers(self):
        """Get headers with authorization"""
        return {
            "Authorization": f"Bearer {self.token}",
            "Content-Type": "application/json"
        }

# Usage
client = AlgoTrendyClient()
client.login("your_username", "your_password")
```

### JavaScript/TypeScript

```javascript
class AlgoTrendyClient {
  constructor(baseUrl = 'http://localhost:5002/api') {
    this.baseUrl = baseUrl;
    this.token = null;
  }

  async login(username, password) {
    const response = await fetch(`${this.baseUrl}/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ username, password })
    });

    if (!response.ok) throw new Error('Login failed');

    const data = await response.json();
    this.token = data.token;
    return this.token;
  }

  getHeaders() {
    return {
      'Authorization': `Bearer ${this.token}`,
      'Content-Type': 'application/json'
    };
  }
}

// Usage
const client = new AlgoTrendyClient();
await client.login('your_username', 'your_password');
```

---

## Market Data

### Get Available Symbols

#### cURL

```bash
curl -X GET http://localhost:5002/api/MarketData/symbols \
  -H "Authorization: Bearer YOUR_TOKEN"
```

#### Python

```python
def get_symbols(client):
    """Get all available trading symbols"""
    response = requests.get(
        f"{client.base_url}/MarketData/symbols",
        headers=client._headers()
    )
    return response.json()

# Usage
symbols = get_symbols(client)
print(f"Available symbols: {symbols}")
```

#### JavaScript

```javascript
async getSymbols() {
  const response = await fetch(`${this.baseUrl}/MarketData/symbols`, {
    headers: this.getHeaders()
  });
  return await response.json();
}

// Usage
const symbols = await client.getSymbols();
console.log('Available symbols:', symbols);
```

### Get Latest Market Data

#### cURL

```bash
curl -X GET "http://localhost:5002/api/MarketData/latest?symbol=BTCUSDT" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

#### Python

```python
def get_latest_price(client, symbol):
    """Get latest market data for a symbol"""
    response = requests.get(
        f"{client.base_url}/MarketData/latest",
        headers=client._headers(),
        params={"symbol": symbol}
    )
    return response.json()

# Usage
btc_data = get_latest_price(client, "BTCUSDT")
print(f"BTC Price: ${btc_data['close']}")
```

#### JavaScript

```javascript
async getLatestPrice(symbol) {
  const response = await fetch(
    `${this.baseUrl}/MarketData/latest?symbol=${symbol}`,
    { headers: this.getHeaders() }
  );
  return await response.json();
}

// Usage
const btcData = await client.getLatestPrice('BTCUSDT');
console.log(`BTC Price: $${btcData.close}`);
```

### Get Historical Data

#### cURL

```bash
curl -X GET "http://localhost:5002/api/MarketData/history?symbol=BTCUSDT&interval=1h&limit=100" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

#### Python

```python
def get_historical_data(client, symbol, interval="1h", limit=100):
    """Get historical market data"""
    response = requests.get(
        f"{client.base_url}/MarketData/history",
        headers=client._headers(),
        params={
            "symbol": symbol,
            "interval": interval,
            "limit": limit
        }
    )
    return response.json()

# Usage
import pandas as pd

history = get_historical_data(client, "BTCUSDT", interval="1h", limit=100)
df = pd.DataFrame(history)
print(df.head())
```

#### JavaScript

```javascript
async getHistoricalData(symbol, interval = '1h', limit = 100) {
  const params = new URLSearchParams({ symbol, interval, limit });
  const response = await fetch(
    `${this.baseUrl}/MarketData/history?${params}`,
    { headers: this.getHeaders() }
  );
  return await response.json();
}

// Usage
const history = await client.getHistoricalData('BTCUSDT', '1h', 100);
console.log(`Retrieved ${history.length} candles`);
```

---

## Orders

### Place Market Order

#### cURL

```bash
curl -X POST http://localhost:5002/api/Orders/market \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "symbol": "BTCUSDT",
    "side": "Buy",
    "quantity": 0.001,
    "brokerId": "bybit"
  }'
```

#### Python

```python
def place_market_order(client, symbol, side, quantity, broker_id="bybit"):
    """Place a market order"""
    response = requests.post(
        f"{client.base_url}/Orders/market",
        headers=client._headers(),
        json={
            "symbol": symbol,
            "side": side,
            "quantity": quantity,
            "brokerId": broker_id
        }
    )
    return response.json()

# Usage
order = place_market_order(
    client,
    symbol="BTCUSDT",
    side="Buy",
    quantity=0.001
)
print(f"Order placed: {order['orderId']}")
```

#### JavaScript

```javascript
async placeMarketOrder(symbol, side, quantity, brokerId = 'bybit') {
  const response = await fetch(`${this.baseUrl}/Orders/market`, {
    method: 'POST',
    headers: this.getHeaders(),
    body: JSON.stringify({ symbol, side, quantity, brokerId })
  });
  return await response.json();
}

// Usage
const order = await client.placeMarketOrder('BTCUSDT', 'Buy', 0.001);
console.log(`Order placed: ${order.orderId}`);
```

### Place Limit Order

#### cURL

```bash
curl -X POST http://localhost:5002/api/Orders/limit \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "symbol": "BTCUSDT",
    "side": "Buy",
    "quantity": 0.001,
    "price": 65000.0,
    "brokerId": "bybit"
  }'
```

#### Python

```python
def place_limit_order(client, symbol, side, quantity, price, broker_id="bybit"):
    """Place a limit order"""
    response = requests.post(
        f"{client.base_url}/Orders/limit",
        headers=client._headers(),
        json={
            "symbol": symbol,
            "side": side,
            "quantity": quantity,
            "price": price,
            "brokerId": broker_id
        }
    )
    return response.json()

# Usage
order = place_limit_order(
    client,
    symbol="BTCUSDT",
    side="Buy",
    quantity=0.001,
    price=65000.0
)
print(f"Limit order placed: {order['orderId']}")
```

### Get Order Status

#### Python

```python
def get_order_status(client, order_id):
    """Get status of an order"""
    response = requests.get(
        f"{client.base_url}/Orders/{order_id}",
        headers=client._headers()
    )
    return response.json()

# Usage
status = get_order_status(client, "order_id_123")
print(f"Order status: {status['status']}")
```

### Cancel Order

#### Python

```python
def cancel_order(client, order_id):
    """Cancel an open order"""
    response = requests.delete(
        f"{client.base_url}/Orders/{order_id}",
        headers=client._headers()
    )
    return response.json()

# Usage
result = cancel_order(client, "order_id_123")
print(f"Cancel result: {result}")
```

---

## Backtesting

### Run Backtest

#### cURL

```bash
curl -X POST http://localhost:5002/api/Backtest/run \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "algorithmCode": "class MyStrategy(QCAlgorithm):\n    def Initialize(self):\n        self.SetStartDate(2023, 1, 1)\n        self.SetCash(100000)\n        self.AddEquity(\"SPY\", Resolution.Daily)",
    "startDate": "2023-01-01",
    "endDate": "2023-12-31",
    "initialCapital": 100000,
    "engineType": "QuantConnect"
  }'
```

#### Python

```python
def run_backtest(client, algorithm_code, start_date, end_date, initial_capital=100000):
    """Run a backtest"""
    response = requests.post(
        f"{client.base_url}/Backtest/run",
        headers=client._headers(),
        json={
            "algorithmCode": algorithm_code,
            "startDate": start_date,
            "endDate": end_date,
            "initialCapital": initial_capital,
            "engineType": "QuantConnect"
        }
    )
    return response.json()

# Usage
algorithm = """
class MyStrategy(QCAlgorithm):
    def Initialize(self):
        self.SetStartDate(2023, 1, 1)
        self.SetCash(100000)
        self.AddEquity("SPY", Resolution.Daily)

    def OnData(self, data):
        if not self.Portfolio.Invested:
            self.SetHoldings("SPY", 1.0)
"""

result = run_backtest(
    client,
    algorithm_code=algorithm,
    start_date="2023-01-01",
    end_date="2023-12-31"
)
print(f"Backtest ID: {result['backtestId']}")
```

#### JavaScript

```javascript
async runBacktest(algorithmCode, startDate, endDate, initialCapital = 100000) {
  const response = await fetch(`${this.baseUrl}/Backtest/run`, {
    method: 'POST',
    headers: this.getHeaders(),
    body: JSON.stringify({
      algorithmCode,
      startDate,
      endDate,
      initialCapital,
      engineType: 'QuantConnect'
    })
  });
  return await response.json();
}

// Usage
const algorithm = `
class MyStrategy(QCAlgorithm):
    def Initialize(self):
        self.SetStartDate(2023, 1, 1)
        self.SetCash(100000)
        self.AddEquity("SPY", Resolution.Daily)
`;

const result = await client.runBacktest(
  algorithm,
  '2023-01-01',
  '2023-12-31'
);
console.log(`Backtest ID: ${result.backtestId}`);
```

### Get Backtest Results

#### Python

```python
def get_backtest_results(client, backtest_id):
    """Get results of a completed backtest"""
    response = requests.get(
        f"{client.base_url}/Backtest/{backtest_id}/results",
        headers=client._headers()
    )
    return response.json()

# Usage
results = get_backtest_results(client, "backtest_id_123")
print(f"Total Return: {results['totalReturn']}%")
print(f"Sharpe Ratio: {results['sharpeRatio']}")
print(f"Max Drawdown: {results['maxDrawdown']}%")
```

---

## Portfolio

### Get Portfolio Summary

#### cURL

```bash
curl -X GET http://localhost:5002/api/Portfolio/summary \
  -H "Authorization: Bearer YOUR_TOKEN"
```

#### Python

```python
def get_portfolio_summary(client):
    """Get portfolio summary"""
    response = requests.get(
        f"{client.base_url}/Portfolio/summary",
        headers=client._headers()
    )
    return response.json()

# Usage
portfolio = get_portfolio_summary(client)
print(f"Total Value: ${portfolio['totalValue']}")
print(f"Available Cash: ${portfolio['availableCash']}")
print(f"Total P&L: ${portfolio['totalPnL']}")
```

#### JavaScript

```javascript
async getPortfolioSummary() {
  const response = await fetch(`${this.baseUrl}/Portfolio/summary`, {
    headers: this.getHeaders()
  });
  return await response.json();
}

// Usage
const portfolio = await client.getPortfolioSummary();
console.log(`Total Value: $${portfolio.totalValue}`);
```

### Get Open Positions

#### Python

```python
def get_open_positions(client):
    """Get all open positions"""
    response = requests.get(
        f"{client.base_url}/Portfolio/positions",
        headers=client._headers()
    )
    return response.json()

# Usage
positions = get_open_positions(client)
for pos in positions:
    print(f"{pos['symbol']}: {pos['quantity']} @ ${pos['avgPrice']}")
```

---

## WebSocket (SignalR)

### Python (using signalrcore)

```python
from signalrcore.hub_connection_builder import HubConnectionBuilder

class MarketDataSubscriber:
    def __init__(self, hub_url, token):
        self.connection = HubConnectionBuilder()\
            .with_url(hub_url, options={
                "access_token_factory": lambda: token
            })\
            .configure_logging(logging.INFO)\
            .with_automatic_reconnect({
                "type": "interval",
                "intervals": [0, 2, 10, 30]
            })\
            .build()

        # Register event handlers
        self.connection.on("ReceiveMarketData", self.on_market_data)
        self.connection.on("ReceiveTrade", self.on_trade)

    def on_market_data(self, data):
        print(f"Market Data: {data}")

    def on_trade(self, trade):
        print(f"Trade: {trade}")

    def start(self):
        self.connection.start()

    def subscribe_to_symbol(self, symbol):
        self.connection.send("SubscribeToSymbol", [symbol])

# Usage
subscriber = MarketDataSubscriber(
    "http://localhost:5002/hubs/market",
    client.token
)
subscriber.start()
subscriber.subscribe_to_symbol("BTCUSDT")
```

### JavaScript (using @microsoft/signalr)

```javascript
import * as signalR from '@microsoft/signalr';

class MarketDataSubscriber {
  constructor(hubUrl, token) {
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, { accessTokenFactory: () => token })
      .withAutomaticReconnect()
      .build();

    // Register event handlers
    this.connection.on('ReceiveMarketData', this.onMarketData.bind(this));
    this.connection.on('ReceiveTrade', this.onTrade.bind(this));
  }

  onMarketData(data) {
    console.log('Market Data:', data);
  }

  onTrade(trade) {
    console.log('Trade:', trade);
  }

  async start() {
    await this.connection.start();
    console.log('SignalR Connected');
  }

  async subscribeToSymbol(symbol) {
    await this.connection.invoke('SubscribeToSymbol', symbol);
  }
}

// Usage
const subscriber = new MarketDataSubscriber(
  'http://localhost:5002/hubs/market',
  client.token
);
await subscriber.start();
await subscriber.subscribeToSymbol('BTCUSDT');
```

---

## Complete Example: Automated Trading Bot

### Python

```python
import requests
import time
from datetime import datetime

class TradingBot:
    def __init__(self, base_url, username, password):
        self.client = AlgoTrendyClient(base_url)
        self.client.login(username, password)

    def run_simple_strategy(self, symbol, quantity):
        """
        Simple moving average crossover strategy
        """
        print(f"Starting strategy for {symbol}...")

        while True:
            # Get historical data
            history = get_historical_data(
                self.client,
                symbol,
                interval="1h",
                limit=50
            )

            # Calculate simple moving averages
            prices = [candle['close'] for candle in history]
            sma_short = sum(prices[-10:]) / 10
            sma_long = sum(prices[-30:]) / 30

            # Get current position
            positions = get_open_positions(self.client)
            has_position = any(p['symbol'] == symbol for p in positions)

            # Trading logic
            if sma_short > sma_long and not has_position:
                print(f"BUY signal detected! SMA Short: {sma_short}, SMA Long: {sma_long}")
                order = place_market_order(self.client, symbol, "Buy", quantity)
                print(f"Order placed: {order}")

            elif sma_short < sma_long and has_position:
                print(f"SELL signal detected! SMA Short: {sma_short}, SMA Long: {sma_long}")
                order = place_market_order(self.client, symbol, "Sell", quantity)
                print(f"Order placed: {order}")

            else:
                print(f"No signal. SMA Short: {sma_short:.2f}, SMA Long: {sma_long:.2f}")

            # Wait before next iteration
            time.sleep(3600)  # 1 hour

# Usage
if __name__ == "__main__":
    bot = TradingBot(
        base_url="http://localhost:5002/api",
        username="your_username",
        password="your_password"
    )
    bot.run_simple_strategy("BTCUSDT", quantity=0.001)
```

---

## Error Handling

### Python

```python
def make_api_request(func):
    """Decorator for API error handling"""
    def wrapper(*args, **kwargs):
        try:
            return func(*args, **kwargs)
        except requests.exceptions.HTTPError as e:
            print(f"HTTP Error: {e.response.status_code}")
            print(f"Response: {e.response.text}")
            raise
        except requests.exceptions.ConnectionError:
            print("Connection Error: Could not connect to API")
            raise
        except requests.exceptions.Timeout:
            print("Timeout Error: Request timed out")
            raise
        except Exception as e:
            print(f"Unexpected error: {e}")
            raise
    return wrapper

@make_api_request
def get_market_data(client, symbol):
    response = requests.get(
        f"{client.base_url}/MarketData/latest",
        headers=client._headers(),
        params={"symbol": symbol},
        timeout=30
    )
    response.raise_for_status()
    return response.json()
```

### JavaScript

```javascript
async function makeApiRequest(requestFunc) {
  try {
    return await requestFunc();
  } catch (error) {
    if (error.response) {
      console.error('HTTP Error:', error.response.status);
      console.error('Response:', await error.response.text());
    } else if (error.request) {
      console.error('No response received:', error.request);
    } else {
      console.error('Error:', error.message);
    }
    throw error;
  }
}

// Usage
const data = await makeApiRequest(() =>
  client.getLatestPrice('BTCUSDT')
);
```

---

## Rate Limiting

The API implements rate limiting to prevent abuse:

- **Default limit:** 100 requests per minute per IP
- **Authenticated:** 1000 requests per minute per user

When rate limited, you'll receive a `429 Too Many Requests` response with a `Retry-After` header.

### Python Rate Limit Handling

```python
import time

def api_call_with_retry(func, max_retries=3):
    """Retry API call with exponential backoff"""
    for attempt in range(max_retries):
        try:
            return func()
        except requests.exceptions.HTTPError as e:
            if e.response.status_code == 429:
                retry_after = int(e.response.headers.get('Retry-After', 60))
                print(f"Rate limited. Retrying after {retry_after} seconds...")
                time.sleep(retry_after)
            else:
                raise
    raise Exception("Max retries exceeded")
```

---

## Additional Resources

- **API Documentation:** http://localhost:5002/swagger
- **Project Repository:** https://github.com/KenyBoi/algotrendy-v2.6
- **Support:** Open an issue on GitHub

---

**Happy Trading!** 🚀
