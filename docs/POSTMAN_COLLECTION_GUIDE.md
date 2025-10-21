# AlgoTrendy API - Postman Collection Guide

**Last Updated:** October 21, 2025
**Collection File:** `AlgoTrendy_API.postman_collection.json`

---

## 🚀 Quick Start

### Step 1: Import Collection

1. **Open Postman** (download from https://www.postman.com if needed)
2. Click **Import** button (top left)
3. Drag and drop `AlgoTrendy_API.postman_collection.json`
4. Click **Import**

### Step 2: Configure Environment

The collection uses two variables:

| Variable | Default Value | Description |
|----------|---------------|-------------|
| `baseUrl` | `http://localhost:5002` | API base URL |
| `apiVersion` | `v1` | API version |

**To change these:**
1. Click on the collection name
2. Go to **Variables** tab
3. Update **Current Value** as needed
4. Click **Save**

### Step 3: Start Testing!

All endpoints are organized into folders:
- 📊 Market Data
- 📋 Orders
- 💼 Positions
- 🔬 Backtesting
- 🤖 ML Training
- 💰 Portfolio
- ❤️ Health & Status

---

## 📁 Collection Structure

### 1. Market Data
Get historical and real-time market data

**Endpoints:**
- `GET /api/v1/marketdata` - Historical OHLCV data
  - **Example:** Get 100 1-hour candles for BTC
  - **Parameters:** symbol, exchange, interval, limit

- `GET /api/v1/marketdata/ticker` - Current ticker
  - **Example:** Get current BTC price and 24h stats
  - **Parameters:** symbol, exchange

**Try it:**
```
GET {{baseUrl}}/api/{{apiVersion}}/marketdata?symbol=BTCUSDT&exchange=binance&interval=1h&limit=100
```

---

### 2. Orders
Place, track, and cancel orders

**Endpoints:**
- `POST /api/v1/orders` - Place order (market or limit)
  - **Market Order Example:**
    ```json
    {
      "symbol": "BTCUSDT",
      "exchange": "binance",
      "side": "Buy",
      "type": "Market",
      "quantity": 0.001
    }
    ```
  - **Limit Order Example:**
    ```json
    {
      "symbol": "BTCUSDT",
      "exchange": "binance",
      "side": "Buy",
      "type": "Limit",
      "quantity": 0.001,
      "price": 50000.00
    }
    ```

- `GET /api/v1/orders/:orderId` - Get order status
- `DELETE /api/v1/orders/:orderId` - Cancel order

**Important:**
- Use **testnet** credentials for testing orders
- Never test with real money initially

---

### 3. Positions
Manage open positions

**Endpoints:**
- `GET /api/v1/positions` - Get all positions
  - **Parameters:** exchange

- `POST /api/v1/positions/close` - Close position
  - **Example:**
    ```json
    {
      "symbol": "BTCUSDT",
      "exchange": "binance",
      "quantity": 0.001
    }
    ```

---

### 4. Backtesting
Run and analyze backtests

**Endpoints:**
- `POST /api/v1/backtest` - Create backtest
  - **Example:**
    ```json
    {
      "name": "My Strategy Backtest",
      "symbol": "BTCUSDT",
      "startDate": "2024-01-01T00:00:00Z",
      "endDate": "2024-12-31T23:59:59Z",
      "initialCapital": 10000,
      "strategyCode": "// Your strategy code here"
    }
    ```

- `GET /api/v1/backtest/:backtestId` - Get results
- `GET /api/v1/backtest` - List all backtests

---

### 5. ML Training
Train models and get predictions

**Endpoints:**
- `POST /api/v1/ml/train` - Train new model
  - **Example:**
    ```json
    {
      "modelType": "TrendReversal",
      "symbols": ["BTCUSDT", "ETHUSDT"],
      "startDate": "2024-01-01",
      "endDate": "2024-12-31",
      "features": [
        "rsi",
        "macd",
        "bollinger_bands",
        "volume_profile"
      ]
    }
    ```

- `GET /api/v1/ml/models/:modelId` - Get model status
- `POST /api/v1/ml/predict` - Get predictions

**Available Model Types:**
- `TrendReversal` - Predict trend reversals
- `PriceDirection` - Predict price direction
- `VolatilityForecast` - Forecast volatility

---

### 6. Portfolio
Track portfolio and balances

**Endpoints:**
- `GET /api/v1/portfolio` - Portfolio summary
- `GET /api/v1/portfolio/balance` - Account balance
  - **Parameters:** exchange

---

### 7. Health & Status
Check API health

**Endpoints:**
- `GET /health` - Health check
- `GET /api/version` - Version info

---

## 🔧 Common Use Cases

### Use Case 1: Get Latest Bitcoin Price

**Request:**
```http
GET {{baseUrl}}/api/{{apiVersion}}/marketdata/ticker?symbol=BTCUSDT&exchange=binance
```

**Expected Response:**
```json
{
  "symbol": "BTCUSDT",
  "price": 65432.10,
  "volume24h": 28567.32,
  "change24h": 2.35,
  "high24h": 66000.00,
  "low24h": 64000.00
}
```

---

### Use Case 2: Place Test Market Order

**Request:**
```http
POST {{baseUrl}}/api/{{apiVersion}}/orders
Content-Type: application/json

{
  "symbol": "BTCUSDT",
  "exchange": "binance",
  "side": "Buy",
  "type": "Market",
  "quantity": 0.001
}
```

**Expected Response:**
```json
{
  "orderId": "12345678",
  "status": "Filled",
  "symbol": "BTCUSDT",
  "side": "Buy",
  "quantity": 0.001,
  "price": 65432.10,
  "timestamp": "2025-10-21T12:00:00Z"
}
```

---

### Use Case 3: Run Simple Backtest

**Request:**
```http
POST {{baseUrl}}/api/{{apiVersion}}/backtest
Content-Type: application/json

{
  "name": "Simple MA Crossover",
  "symbol": "BTCUSDT",
  "startDate": "2024-01-01T00:00:00Z",
  "endDate": "2024-12-31T23:59:59Z",
  "initialCapital": 10000,
  "strategyCode": "// MA crossover strategy"
}
```

**Expected Response:**
```json
{
  "backtestId": 123,
  "status": "Running",
  "estimatedCompletion": "2025-10-21T12:05:00Z"
}
```

Then check results:
```http
GET {{baseUrl}}/api/{{apiVersion}}/backtest/123
```

---

## 🛠️ Advanced Features

### Authentication (When Implemented)

If the API requires authentication:

1. Add to **Headers** in each request:
   ```
   Authorization: Bearer YOUR_API_TOKEN
   ```

2. Or use **Collection Variables**:
   - Add variable `apiToken`
   - Reference as `{{apiToken}}`

### Environment Variables

Create different environments for:
- **Local Development:** `http://localhost:5002`
- **Staging:** `https://staging.algotrendy.com`
- **Production:** `https://api.algotrendy.com`

**To create environment:**
1. Click **Environments** (left sidebar)
2. Click **+** to create new
3. Add variables (baseUrl, apiToken, etc.)
4. Select active environment (top right)

### Test Scripts

Add tests to verify responses:

```javascript
// Test: Check status code
pm.test("Status code is 200", function () {
    pm.response.to.have.status(200);
});

// Test: Response has required fields
pm.test("Response has price", function () {
    var jsonData = pm.response.json();
    pm.expect(jsonData).to.have.property('price');
});

// Test: Save order ID for later
if (pm.response.code === 200) {
    var jsonData = pm.response.json();
    pm.collectionVariables.set("orderId", jsonData.orderId);
}
```

---

## 📊 Response Codes

| Code | Meaning | Action |
|------|---------|--------|
| **200** | Success | Request completed successfully |
| **201** | Created | Resource created (e.g., order placed) |
| **400** | Bad Request | Check request parameters |
| **401** | Unauthorized | Check authentication token |
| **404** | Not Found | Resource doesn't exist |
| **429** | Rate Limited | Wait before retrying |
| **500** | Server Error | Contact support if persists |

---

## 🔍 Troubleshooting

### Connection Refused

**Problem:** `Error: connect ECONNREFUSED`

**Solution:**
1. Ensure API is running: `dotnet run`
2. Check baseUrl matches your API port
3. Verify Docker containers if using Docker

### 404 Not Found

**Problem:** `404 - Not Found`

**Solution:**
1. Check endpoint path is correct
2. Verify API version (v1, v2, etc.)
3. Ensure controller is not disabled

### Invalid JSON

**Problem:** `400 - Invalid JSON`

**Solution:**
1. Validate JSON syntax (use jsonlint.com)
2. Check Content-Type header is `application/json`
3. Ensure all required fields are present

### CORS Errors (Browser)

**Problem:** CORS policy error

**Solution:**
- Use Postman desktop app (not web)
- Or configure CORS in API settings

---

## 📚 Additional Resources

### Documentation
- **[API Usage Examples](API_USAGE_EXAMPLES.md)** - Code examples in Python, JS, C#
- **[Swagger UI](http://localhost:5002/swagger)** - Interactive API documentation
- **[Quick Start Guide](../QUICK_START_GUIDE.md)** - Get started in 5 minutes

### Tutorials
- **Market Data Tutorial** - Fetch and analyze price data
- **Order Placement Tutorial** - Place your first trade
- **Backtesting Tutorial** - Test a trading strategy

### Support
- GitHub Issues: https://github.com/KenyBoi/algotrendy-v2.6/issues
- Documentation: See `docs/` folder
- Swagger UI: http://localhost:5002/swagger

---

## ✅ Testing Checklist

Before using with real money:

- [ ] Test all endpoints with testnet credentials
- [ ] Verify market data retrieval works
- [ ] Place test orders (testnet only)
- [ ] Check order status and cancellation
- [ ] Run a simple backtest
- [ ] Verify portfolio data accuracy
- [ ] Test error handling (invalid symbols, etc.)
- [ ] Review API rate limits
- [ ] Understand all response codes
- [ ] Configure production credentials separately

---

## 🎯 Next Steps

1. **Import the collection** into Postman
2. **Configure baseUrl** to your API endpoint
3. **Test Health Check** endpoint first
4. **Try Market Data** endpoints
5. **Setup testnet credentials** for orders
6. **Explore ML endpoints** for predictions

---

**Happy Testing! 🚀**

For more examples and integration guides, see:
- [API Usage Examples](API_USAGE_EXAMPLES.md) - Multi-language examples
- [Quick Start Guide](../QUICK_START_GUIDE.md) - 1-page reference
- [Swagger UI](http://localhost:5002/swagger) - Interactive docs

---

*Last Updated: October 21, 2025*
*Collection Version: 2.6.0*
