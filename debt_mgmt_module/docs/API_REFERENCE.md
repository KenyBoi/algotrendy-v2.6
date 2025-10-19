# Debt Management Module - API Reference

## Base URL
```
/api/debt_mgmt/v1
```

## Authentication
All endpoints require JWT authentication unless otherwise specified.

```http
Authorization: Bearer <jwt_token>
```

---

## Endpoints

### Portfolio Management

#### GET /portfolio
Get comprehensive portfolio summary including margin and leverage data.

**Response:**
```json
{
  "total_value": 125000.50,
  "cash": 25000.00,
  "equity": 100000.50,
  "buying_power": 50000.00,
  "margin_used": 30000.00,
  "margin_available": 20000.00,
  "margin_utilization": 0.60,
  "unrealized_pnl": 2500.50,
  "realized_pnl": 7500.00,
  "open_positions": 5,
  "total_leverage": 2.0
}
```

**Fields:**
- `total_value`: Total portfolio value (cash + equity)
- `cash`: Available cash balance
- `equity`: Value of open positions
- `buying_power`: Maximum buying power (cash × leverage)
- `margin_used`: Currently used margin
- `margin_available`: Available margin for new positions
- `margin_utilization`: Percentage of margin in use (0.0-1.0)
- `unrealized_pnl`: Unrealized profit/loss
- `realized_pnl`: Realized profit/loss
- `open_positions`: Number of open positions
- `total_leverage`: Overall portfolio leverage

---

#### GET /positions
Get all active trading positions with margin details.

**Query Parameters:**
- `status` (optional): Filter by status (`open`, `closed`, `all`)
- `symbol` (optional): Filter by trading symbol
- `limit` (optional): Maximum results (default: 100)
- `offset` (optional): Pagination offset

**Response:**
```json
{
  "positions": [
    {
      "position_id": "pos_btc_001",
      "symbol": "BTCUSDT",
      "side": "LONG",
      "size": 0.5,
      "entry_price": 65000.00,
      "current_price": 66250.00,
      "unrealized_pnl": 625.00,
      "unrealized_pnl_percent": 1.92,
      "leverage": 2.0,
      "margin": 16250.00,
      "liquidation_price": 48750.00,
      "stop_loss": 63000.00,
      "take_profit": 70000.00,
      "status": "OPEN",
      "broker": "bybit",
      "opened_at": "2025-10-18T10:30:00Z",
      "metadata": {}
    }
  ],
  "total": 5,
  "page": 1,
  "limit": 100
}
```

---

### Margin Management

#### GET /margin/status
Get current margin status and risk metrics.

**Response:**
```json
{
  "margin_used": 30000.00,
  "margin_available": 20000.00,
  "margin_total": 50000.00,
  "margin_utilization": 0.60,
  "margin_level": "SAFE",
  "liquidation_risk": "LOW",
  "maintenance_margin": 24000.00,
  "excess_margin": 6000.00,
  "warnings": [],
  "thresholds": {
    "warning": 0.70,
    "critical": 0.80,
    "liquidation": 0.90
  },
  "time_to_liquidation": null
}
```

**Margin Levels:**
- `SAFE`: < 70% utilization
- `WARNING`: 70-80% utilization
- `CRITICAL`: 80-90% utilization
- `DANGER`: > 90% utilization

---

#### GET /margin/requirements/{symbol}
Calculate margin requirement for a potential trade.

**Path Parameters:**
- `symbol`: Trading pair (e.g., `BTCUSDT`)

**Query Parameters:**
- `side`: Trade side (`BUY`, `SELL`)
- `quantity`: Trade quantity
- `leverage`: Desired leverage (optional, uses default)

**Response:**
```json
{
  "symbol": "BTCUSDT",
  "quantity": 0.5,
  "current_price": 66250.00,
  "notional_value": 33125.00,
  "leverage": 2.0,
  "required_margin": 16562.50,
  "available_margin": 20000.00,
  "can_execute": true,
  "margin_after_trade": 46562.50,
  "utilization_after_trade": 0.93,
  "warnings": [
    "High margin utilization after trade"
  ]
}
```

---

### Leverage Management

#### GET /leverage/{symbol}
Get current leverage setting for a symbol.

**Path Parameters:**
- `symbol`: Trading pair (e.g., `BTCUSDT`)

**Response:**
```json
{
  "symbol": "BTCUSDT",
  "leverage": 2.0,
  "max_leverage": 5.0,
  "broker": "bybit",
  "last_updated": "2025-10-18T10:00:00Z"
}
```

---

#### POST /leverage/set
Set leverage for a trading symbol.

**Request Body:**
```json
{
  "symbol": "BTCUSDT",
  "leverage": 3.0,
  "broker": "bybit"
}
```

**Validation:**
- `leverage` must be between `min_leverage` and `max_leverage`
- Cannot change leverage with open positions (most brokers)
- Rate limited to prevent abuse

**Response:**
```json
{
  "success": true,
  "symbol": "BTCUSDT",
  "leverage": 3.0,
  "previous_leverage": 2.0,
  "broker": "bybit",
  "updated_at": "2025-10-18T11:00:00Z",
  "warnings": [
    "Higher leverage increases liquidation risk"
  ]
}
```

**Error Response (422):**
```json
{
  "success": false,
  "error": "LEVERAGE_TOO_HIGH",
  "message": "Requested leverage 10.0 exceeds maximum 5.0",
  "max_leverage": 5.0
}
```

---

### Fund Management

#### GET /funds
Get current fund status and history.

**Response:**
```json
{
  "available_balance": 25000.00,
  "used_margin": 30000.00,
  "unrealized_pnl": 2500.50,
  "realized_pnl": 7500.00,
  "total_pnl": 10000.50,
  "collateral": 0.00,
  "gross_exposure": 30000.00,
  "last_reset": "2025-10-13T00:00:00Z",
  "reset_count": 5,
  "next_reset": "2025-10-20T00:00:00Z"
}
```

---

#### POST /funds/reset
Manually reset funds (sandbox mode only).

**Request Body:**
```json
{
  "confirm": true,
  "reason": "Testing margin calculations"
}
```

**Response:**
```json
{
  "success": true,
  "new_balance": 10000000.00,
  "previous_balance": 9750000.00,
  "reset_at": "2025-10-18T11:30:00Z",
  "reset_number": 6
}
```

---

#### GET /funds/history
Get fund reset history.

**Query Parameters:**
- `limit` (optional): Maximum results (default: 50)
- `offset` (optional): Pagination offset

**Response:**
```json
{
  "resets": [
    {
      "reset_id": 6,
      "reset_at": "2025-10-18T11:30:00Z",
      "previous_balance": 9750000.00,
      "new_balance": 10000000.00,
      "reason": "Testing margin calculations",
      "reset_type": "manual"
    },
    {
      "reset_id": 5,
      "reset_at": "2025-10-13T00:00:00Z",
      "previous_balance": 9800000.00,
      "new_balance": 10000000.00,
      "reason": "Scheduled weekly reset",
      "reset_type": "automatic"
    }
  ],
  "total": 6
}
```

---

### Liquidation Management

#### GET /liquidations
Get liquidation events and history.

**Query Parameters:**
- `start_date` (optional): Filter by start date
- `end_date` (optional): Filter by end date
- `symbol` (optional): Filter by symbol
- `limit` (optional): Maximum results

**Response:**
```json
{
  "liquidations": [
    {
      "liquidation_id": "liq_001",
      "position_id": "pos_btc_003",
      "symbol": "BTCUSDT",
      "liquidation_price": 48500.00,
      "size": 0.3,
      "loss": -5000.00,
      "margin_released": 12000.00,
      "liquidated_at": "2025-10-15T14:22:00Z",
      "reason": "MARGIN_CALL",
      "broker": "bybit"
    }
  ],
  "total": 1,
  "total_loss": -5000.00
}
```

---

### Health & Monitoring

#### GET /health
Health check endpoint (no authentication required).

**Response:**
```json
{
  "status": "healthy",
  "version": "1.0.0",
  "uptime": 86400,
  "checks": {
    "database": "ok",
    "brokers": "ok",
    "cache": "ok"
  }
}
```

---

#### GET /metrics
Prometheus-compatible metrics endpoint.

**Response (Prometheus format):**
```
# HELP debt_mgmt_margin_utilization Current margin utilization
# TYPE debt_mgmt_margin_utilization gauge
debt_mgmt_margin_utilization 0.60

# HELP debt_mgmt_open_positions Number of open positions
# TYPE debt_mgmt_open_positions gauge
debt_mgmt_open_positions 5

# HELP debt_mgmt_liquidations_total Total liquidation events
# TYPE debt_mgmt_liquidations_total counter
debt_mgmt_liquidations_total 1
```

---

## Error Codes

| Code | Name | Description |
|------|------|-------------|
| 400 | BAD_REQUEST | Invalid request parameters |
| 401 | UNAUTHORIZED | Missing or invalid authentication |
| 403 | FORBIDDEN | Insufficient permissions |
| 404 | NOT_FOUND | Resource not found |
| 422 | VALIDATION_ERROR | Request validation failed |
| 429 | RATE_LIMIT_EXCEEDED | Too many requests |
| 500 | INTERNAL_ERROR | Server error |
| 503 | SERVICE_UNAVAILABLE | Service temporarily unavailable |

**Error Response Format:**
```json
{
  "error": "LEVERAGE_TOO_HIGH",
  "message": "Requested leverage exceeds maximum",
  "details": {
    "requested": 10.0,
    "maximum": 5.0
  },
  "timestamp": "2025-10-18T12:00:00Z"
}
```

---

## Rate Limits

| Endpoint | Limit | Window |
|----------|-------|--------|
| GET /portfolio | 60 req/min | 1 minute |
| GET /positions | 60 req/min | 1 minute |
| POST /leverage/set | 20 req/day | 24 hours |
| POST /funds/reset | 10 req/hour | 1 hour |
| All endpoints | 1000 req/hour | 1 hour |

---

## Webhooks (Future)

Subscribe to real-time events:

### Events
- `margin.warning` - Margin utilization > 70%
- `margin.critical` - Margin utilization > 80%
- `position.liquidated` - Position was liquidated
- `leverage.changed` - Leverage setting changed
- `funds.reset` - Funds were reset

**Webhook Payload:**
```json
{
  "event": "margin.warning",
  "timestamp": "2025-10-18T12:00:00Z",
  "data": {
    "margin_utilization": 0.72,
    "margin_used": 36000.00,
    "margin_available": 14000.00
  }
}
```

---

## SDK Examples

### Python
```python
from debt_mgmt_client import DebtMgmtClient

client = DebtMgmtClient(
    base_url="https://api.example.com",
    api_key="your_api_key"
)

# Get portfolio
portfolio = client.get_portfolio()
print(f"Margin utilization: {portfolio['margin_utilization']:.2%}")

# Set leverage
result = client.set_leverage("BTCUSDT", leverage=3.0)
if result['success']:
    print(f"Leverage updated to {result['leverage']}x")
```

### JavaScript/TypeScript
```typescript
import { DebtMgmtClient } from '@algotrendy/debt-mgmt-sdk';

const client = new DebtMgmtClient({
  baseUrl: 'https://api.example.com',
  apiKey: 'your_api_key'
});

// Get portfolio
const portfolio = await client.getPortfolio();
console.log(`Margin used: ${portfolio.margin_used}`);

// Check margin requirements
const req = await client.getMarginRequirements('BTCUSDT', {
  side: 'BUY',
  quantity: 0.5,
  leverage: 2.0
});

if (req.can_execute) {
  console.log('Trade can be executed');
}
```

---

## Changelog

### v1.0.0 (2025-10-18)
- Initial release
- Portfolio and position endpoints
- Margin management
- Leverage control
- Fund management
- Health monitoring

---

**Last Updated:** 2025-10-18
**API Version:** v1
**Module Version:** 1.0.0
