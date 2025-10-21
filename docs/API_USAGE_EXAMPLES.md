# AlgoTrendy v2.6 - API Usage Examples

Complete guide for integrating with the AlgoTrendy API using various programming languages and tools.

## Table of Contents

- [Getting Started](#getting-started)
- [Authentication](#authentication)
- [cURL Examples](#curl-examples)
- [Python Examples](#python-examples)
- [JavaScript/TypeScript Examples](#javascripttypescript-examples)
- [C# Examples](#c-examples)
- [Postman Collection](#postman-collection)
- [Error Handling](#error-handling)

---

## Getting Started

### Base URL

```
Development:  http://localhost:5002/api
Production:   https://api.algotrendy.com/api
```

### Content Type

All requests and responses use `application/json` unless otherwise specified.

### Rate Limits

- **Market Data**: 1200 requests/minute per IP
- **Trading Operations**: 600 requests/minute per API key
- **Batch Operations**: 100 requests/minute per API key

---

## Authentication

Currently, the API does not require authentication for development. Future versions will use:

- API Key authentication (`X-API-Key` header)
- JWT tokens for user sessions
- OAuth 2.0 for third-party integrations

**Example with API Key** (Future):
```bash
curl -H "X-API-Key: your_api_key_here" \
     https://api.algotrendy.com/api/orders
```

---

## cURL Examples

### 1. Health Check

```bash
curl -X GET http://localhost:5002/health
```

**Response:**
```json
{
  "status": "healthy"
}
```

### 2. Get Market Data

```bash
curl -X GET "http://localhost:5002/api/marketdata/BTCUSDT?interval=1h&limit=100"
```

**Response:**
```json
[
  {
    "symbol": "BTCUSDT",
    "timestamp": "2024-10-21T12:00:00Z",
    "open": 43250.50,
    "high": 43500.00,
    "low": 43100.00,
    "close": 43450.00,
    "volume": 125.5,
    "quoteVolume": 5443125.25
  }
]
```

### 3. Place Market Order

```bash
curl -X POST http://localhost:5002/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "symbol": "BTCUSDT",
    "exchange": "binance",
    "side": "Buy",
    "type": "Market",
    "quantity": 0.001
  }'
```

**Response:**
```json
{
  "orderId": "550e8400-e29b-41d4-a716-446655440000",
  "clientOrderId": "AT_1697123456789_abc123",
  "exchangeOrderId": "12345678901",
  "status": "Filled",
  "message": "Order placed successfully",
  "timestamp": "2024-10-21T12:00:00Z"
}
```

### 4. Place Limit Order

```bash
curl -X POST http://localhost:5002/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "clientOrderId": "MY_CUSTOM_ID_123",
    "symbol": "ETHUSDT",
    "exchange": "binance",
    "side": "Sell",
    "type": "Limit",
    "quantity": 1.5,
    "price": 3000.00
  }'
```

### 5. Get Order Status

```bash
curl -X GET "http://localhost:5002/api/orders/550e8400-e29b-41d4-a716-446655440000"
```

### 6. Get All Orders

```bash
curl -X GET "http://localhost:5002/api/orders?symbol=BTCUSDT&limit=50"
```

### 7. Cancel Order

```bash
curl -X DELETE "http://localhost:5002/api/orders/550e8400-e29b-41d4-a716-446655440000"
```

### 8. Get Account Balance

```bash
curl -X GET "http://localhost:5002/api/account/balance?exchange=binance"
```

### 9. Get Open Positions

```bash
curl -X GET "http://localhost:5002/api/positions?exchange=binance"
```

### 10. Run Backtest

```bash
curl -X POST http://localhost:5002/api/backtest/run \
  -H "Content-Type: application/json" \
  -d '{
    "symbol": "BTCUSDT",
    "startDate": "2024-01-01",
    "endDate": "2024-12-31",
    "initialCapital": 10000,
    "strategyType": "SMA_CROSSOVER",
    "parameters": {
      "fastPeriod": 10,
      "slowPeriod": 30
    }
  }'
```

---

## Python Examples

### Installation

```bash
pip install requests
```

### Basic Client

```python
import requests
from typing import Dict, List, Optional
from datetime import datetime

class AlgoTrendyClient:
    """Python client for AlgoTrendy API"""
    
    def __init__(self, base_url: str = "http://localhost:5002/api"):
        self.base_url = base_url
        self.session = requests.Session()
        self.session.headers.update({
            'Content-Type': 'application/json'
        })
    
    def _get(self, endpoint: str, params: Optional[Dict] = None) -> Dict:
        """Make GET request"""
        response = self.session.get(f"{self.base_url}{endpoint}", params=params)
        response.raise_for_status()
        return response.json()
    
    def _post(self, endpoint: str, data: Dict) -> Dict:
        """Make POST request"""
        response = self.session.post(f"{self.base_url}{endpoint}", json=data)
        response.raise_for_status()
        return response.json()
    
    def _delete(self, endpoint: str) -> Dict:
        """Make DELETE request"""
        response = self.session.delete(f"{self.base_url}{endpoint}")
        response.raise_for_status()
        return response.json()
    
    # Market Data
    def get_market_data(self, symbol: str, interval: str = "1h", limit: int = 100) -> List[Dict]:
        """Get historical market data"""
        return self._get(f"/marketdata/{symbol}", {
            'interval': interval,
            'limit': limit
        })
    
    # Orders
    def place_market_order(self, symbol: str, side: str, quantity: float, 
                          exchange: str = "binance") -> Dict:
        """Place a market order"""
        return self._post("/orders", {
            'symbol': symbol,
            'exchange': exchange,
            'side': side,
            'type': 'Market',
            'quantity': quantity
        })
    
    def place_limit_order(self, symbol: str, side: str, quantity: float, 
                         price: float, exchange: str = "binance") -> Dict:
        """Place a limit order"""
        return self._post("/orders", {
            'symbol': symbol,
            'exchange': exchange,
            'side': side,
            'type': 'Limit',
            'quantity': quantity,
            'price': price
        })
    
    def get_order(self, order_id: str) -> Dict:
        """Get order by ID"""
        return self._get(f"/orders/{order_id}")
    
    def get_orders(self, symbol: Optional[str] = None, limit: int = 100) -> List[Dict]:
        """Get all orders"""
        params = {'limit': limit}
        if symbol:
            params['symbol'] = symbol
        return self._get("/orders", params)
    
    def cancel_order(self, order_id: str) -> Dict:
        """Cancel an order"""
        return self._delete(f"/orders/{order_id}")
    
    # Account
    def get_balance(self, exchange: str = "binance") -> Dict:
        """Get account balance"""
        return self._get(f"/account/balance", {'exchange': exchange})
    
    # Positions
    def get_positions(self, exchange: str = "binance") -> List[Dict]:
        """Get open positions"""
        return self._get("/positions", {'exchange': exchange})
    
    # Backtesting
    def run_backtest(self, symbol: str, start_date: str, end_date: str,
                    initial_capital: float = 10000, strategy_type: str = "SMA_CROSSOVER",
                    parameters: Optional[Dict] = None) -> Dict:
        """Run a backtest"""
        return self._post("/backtest/run", {
            'symbol': symbol,
            'startDate': start_date,
            'endDate': end_date,
            'initialCapital': initial_capital,
            'strategyType': strategy_type,
            'parameters': parameters or {'fastPeriod': 10, 'slowPeriod': 30}
        })

# Usage Example
if __name__ == "__main__":
    client = AlgoTrendyClient()
    
    # Get market data
    print("Fetching BTC market data...")
    market_data = client.get_market_data("BTCUSDT", interval="1h", limit=10)
    print(f"Received {len(market_data)} candles")
    print(f"Latest close: ${market_data[0]['close']}")
    
    # Place market order
    print("\nPlacing market order...")
    order = client.place_market_order(
        symbol="BTCUSDT",
        side="Buy",
        quantity=0.001
    )
    print(f"Order ID: {order['orderId']}")
    print(f"Status: {order['status']}")
    
    # Get order status
    print("\nChecking order status...")
    order_status = client.get_order(order['orderId'])
    print(f"Order status: {order_status['status']}")
    
    # Run backtest
    print("\nRunning backtest...")
    backtest = client.run_backtest(
        symbol="BTCUSDT",
        start_date="2024-01-01",
        end_date="2024-12-31",
        initial_capital=10000
    )
    print(f"Backtest ID: {backtest['backtestId']}")
    print(f"Total Return: {backtest['totalReturn']:.2%}")
    print(f"Sharpe Ratio: {backtest['sharpeRatio']:.2f}")
```

### Async Python Client

```python
import aiohttp
import asyncio
from typing import Dict, List, Optional

class AsyncAlgoTrendyClient:
    """Async Python client for AlgoTrendy API"""
    
    def __init__(self, base_url: str = "http://localhost:5002/api"):
        self.base_url = base_url
    
    async def _get(self, session: aiohttp.ClientSession, endpoint: str, 
                   params: Optional[Dict] = None) -> Dict:
        async with session.get(f"{self.base_url}{endpoint}", params=params) as response:
            response.raise_for_status()
            return await response.json()
    
    async def _post(self, session: aiohttp.ClientSession, endpoint: str, data: Dict) -> Dict:
        async with session.post(f"{self.base_url}{endpoint}", json=data) as response:
            response.raise_for_status()
            return await response.json()
    
    async def get_market_data(self, symbol: str, interval: str = "1h") -> List[Dict]:
        async with aiohttp.ClientSession() as session:
            return await self._get(session, f"/marketdata/{symbol}", {'interval': interval})
    
    async def place_order(self, symbol: str, side: str, quantity: float) -> Dict:
        async with aiohttp.ClientSession() as session:
            return await self._post(session, "/orders", {
                'symbol': symbol,
                'side': side,
                'type': 'Market',
                'quantity': quantity
            })

# Usage
async def main():
    client = AsyncAlgoTrendyClient()
    
    # Fetch multiple symbols concurrently
    symbols = ["BTCUSDT", "ETHUSDT", "SOLUSDT"]
    tasks = [client.get_market_data(symbol) for symbol in symbols]
    results = await asyncio.gather(*tasks)
    
    for symbol, data in zip(symbols, results):
        print(f"{symbol}: ${data[0]['close']}")

asyncio.run(main())
```

---

## JavaScript/TypeScript Examples

### Installation

```bash
npm install axios
```

### TypeScript Client

```typescript
import axios, { AxiosInstance, AxiosResponse } from 'axios';

interface MarketData {
  symbol: string;
  timestamp: string;
  open: number;
  high: number;
  low: number;
  close: number;
  volume: number;
  quoteVolume: number;
}

interface Order {
  orderId: string;
  clientOrderId?: string;
  exchangeOrderId?: string;
  symbol: string;
  exchange: string;
  side: 'Buy' | 'Sell';
  type: 'Market' | 'Limit' | 'StopLoss' | 'StopLimit';
  status: string;
  quantity: number;
  price?: number;
  filledQuantity?: number;
  averageFillPrice?: number;
  timestamp: string;
}

interface OrderRequest {
  clientOrderId?: string;
  symbol: string;
  exchange?: string;
  side: 'Buy' | 'Sell';
  type: 'Market' | 'Limit' | 'StopLoss' | 'StopLimit';
  quantity: number;
  price?: number;
  stopPrice?: number;
}

class AlgoTrendyClient {
  private client: AxiosInstance;

  constructor(baseURL: string = 'http://localhost:5002/api') {
    this.client = axios.create({
      baseURL,
      headers: {
        'Content-Type': 'application/json',
      },
    });
  }

  // Market Data
  async getMarketData(
    symbol: string,
    interval: string = '1h',
    limit: number = 100
  ): Promise<MarketData[]> {
    const response = await this.client.get<MarketData[]>(
      `/marketdata/${symbol}`,
      {
        params: { interval, limit },
      }
    );
    return response.data;
  }

  // Orders
  async placeOrder(orderRequest: OrderRequest): Promise<Order> {
    const response = await this.client.post<Order>('/orders', {
      ...orderRequest,
      exchange: orderRequest.exchange || 'binance',
    });
    return response.data;
  }

  async placeMarketOrder(
    symbol: string,
    side: 'Buy' | 'Sell',
    quantity: number,
    exchange: string = 'binance'
  ): Promise<Order> {
    return this.placeOrder({
      symbol,
      side,
      type: 'Market',
      quantity,
      exchange,
    });
  }

  async placeLimitOrder(
    symbol: string,
    side: 'Buy' | 'Sell',
    quantity: number,
    price: number,
    exchange: string = 'binance'
  ): Promise<Order> {
    return this.placeOrder({
      symbol,
      side,
      type: 'Limit',
      quantity,
      price,
      exchange,
    });
  }

  async getOrder(orderId: string): Promise<Order> {
    const response = await this.client.get<Order>(`/orders/${orderId}`);
    return response.data;
  }

  async getOrders(symbol?: string, limit: number = 100): Promise<Order[]> {
    const response = await this.client.get<Order[]>('/orders', {
      params: { symbol, limit },
    });
    return response.data;
  }

  async cancelOrder(orderId: string): Promise<void> {
    await this.client.delete(`/orders/${orderId}`);
  }

  // Account
  async getBalance(exchange: string = 'binance'): Promise<any> {
    const response = await this.client.get('/account/balance', {
      params: { exchange },
    });
    return response.data;
  }

  // Positions
  async getPositions(exchange: string = 'binance'): Promise<any[]> {
    const response = await this.client.get('/positions', {
      params: { exchange },
    });
    return response.data;
  }
}

// Usage Example
const client = new AlgoTrendyClient();

// Get market data
client.getMarketData('BTCUSDT', '1h', 10)
  .then(data => {
    console.log(`Latest BTC price: $${data[0].close}`);
  })
  .catch(error => {
    console.error('Error:', error.message);
  });

// Place market order
client.placeMarketOrder('BTCUSDT', 'Buy', 0.001)
  .then(order => {
    console.log(`Order placed: ${order.orderId}`);
    console.log(`Status: ${order.status}`);
  })
  .catch(error => {
    console.error('Order failed:', error.message);
  });

// Using async/await
async function tradingExample() {
  try {
    // Fetch market data
    const marketData = await client.getMarketData('BTCUSDT');
    const currentPrice = marketData[0].close;
    console.log(`Current BTC price: $${currentPrice}`);

    // Place buy order if price is favorable
    if (currentPrice < 45000) {
      const order = await client.placeMarketOrder('BTCUSDT', 'Buy', 0.001);
      console.log(`Buy order executed: ${order.orderId}`);
    }

    // Check positions
    const positions = await client.getPositions();
    console.log(`Open positions: ${positions.length}`);
  } catch (error) {
    console.error('Trading error:', error);
  }
}

tradingExample();
```

### React Hook Example

```typescript
import { useState, useEffect } from 'react';
import { AlgoTrendyClient } from './AlgoTrendyClient';

function useMarketData(symbol: string, interval: string = '1h') {
  const [data, setData] = useState<MarketData[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<Error | null>(null);

  useEffect(() => {
    const client = new AlgoTrendyClient();
    
    async function fetchData() {
      try {
        setLoading(true);
        const result = await client.getMarketData(symbol, interval);
        setData(result);
        setError(null);
      } catch (err) {
        setError(err as Error);
      } finally {
        setLoading(false);
      }
    }

    fetchData();
    const interval = setInterval(fetchData, 60000); // Refresh every minute

    return () => clearInterval(interval);
  }, [symbol, interval]);

  return { data, loading, error };
}

// Component usage
function TradingDashboard() {
  const { data, loading, error } = useMarketData('BTCUSDT');

  if (loading) return <div>Loading...</div>;
  if (error) return <div>Error: {error.message}</div>;

  return (
    <div>
      <h1>BTC/USDT</h1>
      <p>Price: ${data[0]?.close}</p>
      <p>Volume: {data[0]?.volume}</p>
    </div>
  );
}
```

---

## C# Examples

### Installation

```bash
dotnet add package RestSharp
```

### C# Client

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RestSharp;
using Newtonsoft.Json;

public class AlgoTrendyClient
{
    private readonly RestClient _client;
    private readonly string _baseUrl;

    public AlgoTrendyClient(string baseUrl = "http://localhost:5002/api")
    {
        _baseUrl = baseUrl;
        _client = new RestClient(baseUrl);
    }

    // Market Data
    public async Task<List<MarketData>> GetMarketDataAsync(
        string symbol, 
        string interval = "1h", 
        int limit = 100)
    {
        var request = new RestRequest($"/marketdata/{symbol}")
            .AddQueryParameter("interval", interval)
            .AddQueryParameter("limit", limit.ToString());

        var response = await _client.ExecuteGetAsync<List<MarketData>>(request);
        
        if (!response.IsSuccessful)
            throw new Exception($"API Error: {response.ErrorMessage}");

        return response.Data;
    }

    // Orders
    public async Task<Order> PlaceMarketOrderAsync(
        string symbol,
        string side,
        decimal quantity,
        string exchange = "binance")
    {
        var request = new RestRequest("/orders", Method.Post)
            .AddJsonBody(new
            {
                symbol,
                exchange,
                side,
                type = "Market",
                quantity
            });

        var response = await _client.ExecutePostAsync<Order>(request);

        if (!response.IsSuccessful)
            throw new Exception($"Order failed: {response.ErrorMessage}");

        return response.Data;
    }

    public async Task<Order> PlaceLimitOrderAsync(
        string symbol,
        string side,
        decimal quantity,
        decimal price,
        string exchange = "binance")
    {
        var request = new RestRequest("/orders", Method.Post)
            .AddJsonBody(new
            {
                symbol,
                exchange,
                side,
                type = "Limit",
                quantity,
                price
            });

        var response = await _client.ExecutePostAsync<Order>(request);

        if (!response.IsSuccessful)
            throw new Exception($"Order failed: {response.ErrorMessage}");

        return response.Data;
    }

    public async Task<Order> GetOrderAsync(string orderId)
    {
        var request = new RestRequest($"/orders/{orderId}");
        var response = await _client.ExecuteGetAsync<Order>(request);

        if (!response.IsSuccessful)
            throw new Exception($"Failed to get order: {response.ErrorMessage}");

        return response.Data;
    }

    public async Task<List<Order>> GetOrdersAsync(string symbol = null, int limit = 100)
    {
        var request = new RestRequest("/orders")
            .AddQueryParameter("limit", limit.ToString());

        if (!string.IsNullOrEmpty(symbol))
            request.AddQueryParameter("symbol", symbol);

        var response = await _client.ExecuteGetAsync<List<Order>>(request);

        if (!response.IsSuccessful)
            throw new Exception($"Failed to get orders: {response.ErrorMessage}");

        return response.Data;
    }

    public async Task CancelOrderAsync(string orderId)
    {
        var request = new RestRequest($"/orders/{orderId}", Method.Delete);
        var response = await _client.ExecuteAsync(request);

        if (!response.IsSuccessful)
            throw new Exception($"Failed to cancel order: {response.ErrorMessage}");
    }
}

// Models
public class MarketData
{
    public string Symbol { get; set; }
    public DateTime Timestamp { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public decimal Volume { get; set; }
    public decimal QuoteVolume { get; set; }
}

public class Order
{
    public string OrderId { get; set; }
    public string ClientOrderId { get; set; }
    public string ExchangeOrderId { get; set; }
    public string Symbol { get; set; }
    public string Exchange { get; set; }
    public string Side { get; set; }
    public string Type { get; set; }
    public string Status { get; set; }
    public decimal Quantity { get; set; }
    public decimal? Price { get; set; }
    public decimal FilledQuantity { get; set; }
    public decimal? AverageFillPrice { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

// Usage Example
class Program
{
    static async Task Main(string[] args)
    {
        var client = new AlgoTrendyClient();

        try
        {
            // Get market data
            Console.WriteLine("Fetching market data...");
            var marketData = await client.GetMarketDataAsync("BTCUSDT", "1h", 10);
            Console.WriteLine($"Latest BTC price: ${marketData[0].Close}");

            // Place market order
            Console.WriteLine("\nPlacing market order...");
            var order = await client.PlaceMarketOrderAsync(
                symbol: "BTCUSDT",
                side: "Buy",
                quantity: 0.001m
            );
            Console.WriteLine($"Order ID: {order.OrderId}");
            Console.WriteLine($"Status: {order.Status}");

            // Get order status
            Console.WriteLine("\nChecking order status...");
            var orderStatus = await client.GetOrderAsync(order.OrderId);
            Console.WriteLine($"Current status: {orderStatus.Status}");

            // Get all orders
            Console.WriteLine("\nFetching all orders...");
            var orders = await client.GetOrdersAsync("BTCUSDT", 50);
            Console.WriteLine($"Total orders: {orders.Count}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
```

---

## Postman Collection

**Ready-to-use Postman collection available!**

📮 **Download:** [AlgoTrendy_API.postman_collection.json](../AlgoTrendy_API.postman_collection.json)
📖 **Guide:** [Postman Collection Guide](POSTMAN_COLLECTION_GUIDE.md)

**Quick Import:**
1. Open Postman
2. Click **Import**
3. Drag and drop `AlgoTrendy_API.postman_collection.json`
4. Start testing immediately!

The collection includes:
- ✅ All API endpoints (market data, orders, backtesting, ML)
- ✅ Pre-configured request examples
- ✅ Environment variables (baseUrl, apiVersion)
- ✅ Organized folders by feature
- ✅ Ready for immediate use

For detailed instructions, see the [Postman Collection Guide](POSTMAN_COLLECTION_GUIDE.md).

---

### Sample Collection Structure (Legacy Reference)

Import this JSON into Postman to get started quickly:

```json
{
  "info": {
    "name": "AlgoTrendy API",
    "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
  },
  "item": [
    {
      "name": "Market Data",
      "request": {
        "method": "GET",
        "header": [],
        "url": {
          "raw": "{{baseUrl}}/marketdata/BTCUSDT?interval=1h&limit=100",
          "host": ["{{baseUrl}}"],
          "path": ["marketdata", "BTCUSDT"],
          "query": [
            {"key": "interval", "value": "1h"},
            {"key": "limit", "value": "100"}
          ]
        }
      }
    },
    {
      "name": "Place Market Order",
      "request": {
        "method": "POST",
        "header": [{"key": "Content-Type", "value": "application/json"}],
        "body": {
          "mode": "raw",
          "raw": "{\n  \"symbol\": \"BTCUSDT\",\n  \"exchange\": \"binance\",\n  \"side\": \"Buy\",\n  \"type\": \"Market\",\n  \"quantity\": 0.001\n}"
        },
        "url": {
          "raw": "{{baseUrl}}/orders",
          "host": ["{{baseUrl}}"],
          "path": ["orders"]
        }
      }
    }
  ],
  "variable": [
    {
      "key": "baseUrl",
      "value": "http://localhost:5002/api"
    }
  ]
}
```

---

## Error Handling

### Common Error Codes

| Code | Meaning | Action |
|------|---------|--------|
| 400 | Bad Request | Check request parameters |
| 404 | Not Found | Verify resource ID/symbol |
| 429 | Rate Limit | Wait and retry |
| 500 | Server Error | Contact support |

### Error Response Format

```json
{
  "error": "Validation failed",
  "details": [
    {
      "field": "quantity",
      "message": "Quantity must be greater than 0"
    }
  ],
  "timestamp": "2024-10-21T12:00:00Z"
}
```

### Python Error Handling

```python
try:
    order = client.place_market_order("BTCUSDT", "Buy", 0.001)
except requests.exceptions.HTTPError as e:
    if e.response.status_code == 400:
        error_data = e.response.json()
        print(f"Validation error: {error_data['error']}")
        for detail in error_data.get('details', []):
            print(f"  - {detail['field']}: {detail['message']}")
    elif e.response.status_code == 429:
        print("Rate limit exceeded. Wait 60 seconds.")
        time.sleep(60)
    else:
        print(f"API error: {e}")
except requests.exceptions.ConnectionError:
    print("Cannot connect to API. Is the server running?")
```

---

## Next Steps

- **Explore Swagger UI**: http://localhost:5002/swagger
- **Read the Architecture**: [ARCHITECTURE.md](ARCHITECTURE.md)
- **Deploy to Production**: [DEPLOYMENT_GUIDE.md](DEPLOYMENT_GUIDE.md)

---

**Last Updated**: October 21, 2025
**API Version**: 2.6.0
